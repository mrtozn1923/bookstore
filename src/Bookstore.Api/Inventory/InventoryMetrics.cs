using System.Diagnostics.Metrics;
using Bookstore.Data;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Inventory;

// A Gauge<T> holds "the current value" and is set from outside. The value needs a database query,
// so a background service measures it every 30 seconds instead of an observable callback doing I/O.
public sealed class InventoryMetrics(IMeterFactory meterFactory, IServiceScopeFactory scopes, ILogger<InventoryMetrics> logger)
    : BackgroundService
{
    public const string MeterName = "Bookstore.Inventory";
    private const int LowStockThreshold = 5;

    private readonly Gauge<int> lowStockBooks = meterFactory.Create(MeterName).CreateGauge<int>(
        "bookstore.inventory.low_stock_books", unit: "{book}", description: $"Books with fewer than {LowStockThreshold} available copies.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromSeconds(30));
        do
        {
            try
            {
                await using AsyncServiceScope scope = scopes.CreateAsyncScope();
                BookstoreContext db = scope.ServiceProvider.GetRequiredService<BookstoreContext>();
                int count = await db.StockItems.CountAsync(s => s.QuantityOnHand - s.QuantityReserved < LowStockThreshold, stoppingToken);
                lowStockBooks.Record(count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Could not measure low stock");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
