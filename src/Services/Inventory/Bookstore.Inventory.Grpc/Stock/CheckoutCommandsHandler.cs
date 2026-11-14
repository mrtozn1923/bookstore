using Bookstore.Contracts.Inventory;
using Bookstore.Inventory.Data;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Inventory.Stock;

// The checkout saga's commands. Every handler is idempotent per OrderId: a redelivered command
// gets the same answer and does not move stock twice.
public static class CheckoutCommandsHandler
{
    public static async Task<object> Handle(ReserveStock command, StockReservations reservations, CancellationToken ct)
    {
        ReservationOutcome outcome = await reservations.ReserveAsync(
            command.OrderId, [.. command.Lines.Select(l => new StockRequest(l.Isbn, l.Quantity))], ct);

        // The answer is an event on Inventory's exchange; Wolverine sends it through the outbox.
        return outcome switch
        {
            ReservationOutcome.Reserved reserved => new StockReserved(
                command.OrderId, reserved.Reservation.Id, new DateTimeOffset(reserved.Reservation.ExpiresAtUtc, TimeSpan.Zero)),
            ReservationOutcome.Unavailable unavailable => new StockReservationFailed(command.OrderId, unavailable.Isbns),
            _ => throw new InvalidOperationException($"Unexpected outcome {outcome}")
        };
    }

    public static async Task<StockCommitted> Handle(CommitStock command, StockReservations reservations, CancellationToken ct) =>
        await reservations.CommitAsync(command.OrderId, ct)
            ? new StockCommitted(command.OrderId)
            : throw new InvalidOperationException($"Order {command.OrderId} has no active reservation to commit.");

    public static async Task Handle(
        ReleaseStock command, InventoryDbContext db, StockReservations reservations, ILogger logger, CancellationToken ct)
    {
        StockReservation? reservation = await db.Reservations.FirstOrDefaultAsync(r => r.OrderId == command.OrderId, ct);
        if (reservation is null)
        {
            logger.LogInformation("No reservation for order {OrderId}; nothing to release", command.OrderId);
            return;
        }
        // Returns 0 when the reservation is no longer active: the second ReleaseStock changes nothing.
        await reservations.ReleaseAsync(reservation, ReservationStatus.Released, ct);
    }
}
