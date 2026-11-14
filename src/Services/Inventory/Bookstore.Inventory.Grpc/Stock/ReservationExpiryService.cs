using Bookstore.Inventory.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Bookstore.Inventory.Stock;

// Puts stock back on sale when an order never confirms its reservation.
public sealed class ReservationExpiryService(
    IServiceScopeFactory scopes, TimeProvider clock, IOptions<ReservationOptions> options, ILogger<ReservationExpiryService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(options.Value.SweepInterval, clock);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ExpireDueReservationsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Expiring reservations failed; trying again on the next tick");
            }
        }
    }

    private async Task ExpireDueReservationsAsync(CancellationToken ct)
    {
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        InventoryDbContext db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        StockReservations reservations = scope.ServiceProvider.GetRequiredService<StockReservations>();

        DateTime now = clock.GetUtcNow().UtcDateTime;
        List<StockReservation> due = await db.Reservations
            .Where(r => r.Status == ReservationStatus.Active && r.ExpiresAtUtc <= now)
            .OrderBy(r => r.ExpiresAtUtc)
            .Take(100)
            .ToListAsync(ct);

        foreach (StockReservation reservation in due)
        {
            await reservations.ReleaseAsync(reservation, ReservationStatus.Expired, ct);
        }
    }
}
