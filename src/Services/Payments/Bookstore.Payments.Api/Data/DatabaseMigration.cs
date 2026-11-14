using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Payments.Data;

// Copied from Catalog on purpose (post 02): no shared "migrations" library.
public static class DatabaseMigration
{
    // The same binary runs as a short-lived migrator: `dotnet run -- --migrate`.
    public static bool IsMigrationRun(string[] args) => args.Contains("--migrate");

    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        CancellationToken ct = app.Lifetime.ApplicationStopping;
        Stopwatch elapsed = Stopwatch.StartNew();

        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        PaymentsDbContext db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

        string[] pending = [.. await db.Database.GetPendingMigrationsAsync(ct)];
        await db.Database.MigrateAsync(ct);
        app.Logger.LogInformation("Applied {Count} migration(s): {Migrations}", pending.Length, string.Join(", ", pending));

        app.Logger.LogInformation("paymentsdb is up to date in {Elapsed} ms", elapsed.ElapsedMilliseconds);
    }
}
