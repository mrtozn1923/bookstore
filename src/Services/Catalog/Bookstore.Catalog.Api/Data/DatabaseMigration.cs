using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Catalog.Data;

public static class DatabaseMigration
{
    // The same binary runs as a short-lived migrator: `dotnet run -- --migrate`.
    public static bool IsMigrationRun(string[] args) => args.Contains("--migrate");

    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        CancellationToken ct = app.Lifetime.ApplicationStopping;
        Stopwatch elapsed = Stopwatch.StartNew();

        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        CatalogDbContext db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        string[] pending = [.. await db.Database.GetPendingMigrationsAsync(ct)];
        await db.Database.MigrateAsync(ct);
        app.Logger.LogInformation("Applied {Count} migration(s): {Migrations}", pending.Length, string.Join(", ", pending));

        if (app.Configuration.GetValue<bool>("Seed:SampleData"))
        {
            await CatalogSeed.SeedAsync(db, ct);
            app.Logger.LogInformation("Sample data ready: {Books} books", await db.Books.CountAsync(ct));
        }

        app.Logger.LogInformation("catalogdb is up to date in {Elapsed} ms", elapsed.ElapsedMilliseconds);
    }
}
