using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Inventory.Data;

public static class DatabaseMigration
{
    public static bool IsMigrationRun(string[] args) => args.Contains("--migrate");

    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        CancellationToken ct = app.Lifetime.ApplicationStopping;
        Stopwatch elapsed = Stopwatch.StartNew();

        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        InventoryDbContext db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        string[] pending = [.. await db.Database.GetPendingMigrationsAsync(ct)];
        await db.Database.MigrateAsync(ct);
        app.Logger.LogInformation("Applied {Count} migration(s): {Migrations}", pending.Length, string.Join(", ", pending));

        if (app.Configuration.GetValue<bool>("Seed:SampleData"))
        {
            await InventorySeed.SeedAsync(db, TimeProvider.System, ct);
            app.Logger.LogInformation("Sample stock ready: {Items} items", await db.StockItems.CountAsync(ct));
        }

        app.Logger.LogInformation("inventorydb is up to date in {Elapsed} ms", elapsed.ElapsedMilliseconds);
    }
}
