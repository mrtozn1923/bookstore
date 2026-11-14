using Bookstore.Inventory.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Bookstore.Inventory.Stock;

public sealed record StockRequest(string Isbn, int Quantity);

// The same reservation logic serves two doors: the gRPC API and the checkout saga's ReserveStock message.
public abstract record ReservationOutcome
{
    public sealed record Reserved(StockReservation Reservation) : ReservationOutcome;
    public sealed record Unavailable(IReadOnlyList<string> Isbns) : ReservationOutcome;
}

public sealed class StockReservations(
    InventoryDbContext db, TimeProvider clock, IOptions<ReservationOptions> options, ILogger<StockReservations> logger)
{
    private const int MaxAttempts = 5;

    public async Task<ReservationOutcome> ReserveAsync(Guid orderId, IReadOnlyList<StockRequest> lines, CancellationToken ct)
    {
        Dictionary<string, int> wanted = lines.GroupBy(l => l.Isbn).ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));

        for (int attempt = 1; ; attempt++)
        {
            // Idempotent: a retried call for the same order gets the reservation it already has.
            StockReservation? existing = await db.Reservations.AsNoTracking().FirstOrDefaultAsync(r => r.OrderId == orderId, ct);
            if (existing is not null)
            {
                return new ReservationOutcome.Reserved(existing);
            }

            try
            {
                return await TryReserveAsync(orderId, wanted, ct);
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxAttempts)
            {
                // Another reservation changed one of these rows after we read it: read again and retry.
                logger.LogWarning("Concurrency conflict for order {OrderId} on attempt {Attempt}, retrying", orderId, attempt);
                db.ChangeTracker.Clear();
                // A short random pause so that the competing requests do not collide again in lockstep.
                await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(10, 50) * attempt), ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
                // The same order was reserved by a parallel call; the next loop returns that reservation.
                db.ChangeTracker.Clear();
            }
        }
    }

    public async Task<int> ReleaseAsync(StockReservation reservation, ReservationStatus newStatus, CancellationToken ct)
    {
        if (reservation.Status != ReservationStatus.Active)
        {
            return 0;
        }

        string[] isbns = [.. reservation.Lines.Select(l => l.Isbn)];
        List<StockItem> items = await db.StockItems.Where(s => isbns.Contains(s.Isbn)).ToListAsync(ct);
        DateTime now = clock.GetUtcNow().UtcDateTime;
        foreach (ReservationLine line in reservation.Lines)
        {
            StockItem item = items.Single(i => i.Isbn == line.Isbn);
            item.QuantityReserved -= line.Quantity;
            item.UpdatedAtUtc = now;
        }
        reservation.Status = newStatus;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Reservation {ReservationId} for order {OrderId} {Status}", reservation.Id, reservation.OrderId, newStatus);
        return reservation.Lines.Count;
    }

    // Turns the reservation into a sale: the copies leave the shelf for good. Idempotent per order.
    public async Task<bool> CommitAsync(Guid orderId, CancellationToken ct)
    {
        StockReservation? reservation = await db.Reservations.FirstOrDefaultAsync(r => r.OrderId == orderId, ct);
        if (reservation is null || reservation.Status is not (ReservationStatus.Active or ReservationStatus.Committed))
        {
            return false;
        }
        if (reservation.Status == ReservationStatus.Committed)
        {
            return true;
        }

        string[] isbns = [.. reservation.Lines.Select(l => l.Isbn)];
        List<StockItem> items = await db.StockItems.Where(s => isbns.Contains(s.Isbn)).ToListAsync(ct);
        DateTime now = clock.GetUtcNow().UtcDateTime;
        foreach (ReservationLine line in reservation.Lines)
        {
            StockItem item = items.Single(i => i.Isbn == line.Isbn);
            item.QuantityOnHand -= line.Quantity;
            item.QuantityReserved -= line.Quantity;
            item.UpdatedAtUtc = now;
        }
        reservation.Status = ReservationStatus.Committed;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Reservation {ReservationId} for order {OrderId} Committed", reservation.Id, orderId);
        return true;
    }

    private async Task<ReservationOutcome> TryReserveAsync(Guid orderId, Dictionary<string, int> wanted, CancellationToken ct)
    {
        List<StockItem> items = await db.StockItems.Where(s => wanted.Keys.Contains(s.Isbn)).ToListAsync(ct);

        string[] unavailable =
        [
            .. wanted.Where(w => items.Find(i => i.Isbn == w.Key) is not { } item || item.Available < w.Value).Select(w => w.Key)
        ];
        if (unavailable.Length > 0)
        {
            return new ReservationOutcome.Unavailable(unavailable);
        }

        DateTime now = clock.GetUtcNow().UtcDateTime;
        foreach (StockItem item in items)
        {
            item.QuantityReserved += wanted[item.Isbn];
            item.UpdatedAtUtc = now;
        }

        StockReservation reservation = new()
        {
            Id = Guid.CreateVersion7(),
            OrderId = orderId,
            Status = ReservationStatus.Active,
            CreatedAtUtc = now,
            ExpiresAtUtc = now + options.Value.Ttl
        };
        reservation.Lines.AddRange(wanted.Select(w => new ReservationLine { Isbn = w.Key, Quantity = w.Value }));
        db.Reservations.Add(reservation);

        // UPDATE ... WHERE [Isbn] = @p AND [RowVersion] = @p for every stock row, plus the INSERT, in one transaction.
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Reserved {Lines} line(s) for order {OrderId} until {ExpiresAtUtc:O}", wanted.Count, orderId, reservation.ExpiresAtUtc);
        return new ReservationOutcome.Reserved(reservation);
    }
}
