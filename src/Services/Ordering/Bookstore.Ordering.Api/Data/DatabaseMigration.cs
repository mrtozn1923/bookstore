using System.Diagnostics;
using Bookstore.Ordering.CatalogCopy;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Ordering.Data;

// Copied from Catalog on purpose: two small copies are cheaper than a shared "migrations" library.
public static class DatabaseMigration
{
    public static bool IsMigrationRun(string[] args) => args.Contains("--migrate");

    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        CancellationToken ct = app.Lifetime.ApplicationStopping;
        Stopwatch elapsed = Stopwatch.StartNew();

        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        OrderingDbContext db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();

        string[] pending = [.. await db.Database.GetPendingMigrationsAsync(ct)];
        await db.Database.MigrateAsync(ct);
        app.Logger.LogInformation("Applied {Count} migration(s): {Migrations}", pending.Length, string.Join(", ", pending));

        // The initial load of the book copy. Afterwards only Catalog's events change it.
        if (app.Configuration.GetValue<bool>("CatalogCopy:ImportWhenEmpty") && !await db.CatalogBooks.AnyAsync(ct))
        {
            CatalogClient catalog = scope.ServiceProvider.GetRequiredService<CatalogClient>();
            DateTime now = TimeProvider.System.GetUtcNow().UtcDateTime;
            await foreach (CatalogBookDto book in catalog.GetAllBooksAsync(ct))
            {
                db.CatalogBooks.Add(new CatalogBook
                {
                    Isbn = book.Isbn,
                    Title = book.Title,
                    Price = book.Price,
                    Currency = book.Currency,
                    Version = book.Version,
                    UpdatedAtUtc = now
                });
            }
            int imported = await db.SaveChangesAsync(ct);
            app.Logger.LogInformation("Imported {Count} books from Catalog into the local copy", imported);
        }

        app.Logger.LogInformation("orderingdb is up to date in {Elapsed} ms", elapsed.ElapsedMilliseconds);
    }
}
