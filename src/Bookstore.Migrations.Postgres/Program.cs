using Bookstore.Data;
using Bookstore.Migrations.Postgres;
using Microsoft.EntityFrameworkCore;

// Applies pending PostgreSQL migrations and exits: 0 = success, 1 = failure.
// Connection string: ConnectionStrings__BookstoreDb environment variable.
await using BookstoreContext db = new DesignTimeContextFactory().CreateDbContext(args);

try
{
    List<string> pending = [.. await db.Database.GetPendingMigrationsAsync()];
    Console.WriteLine(pending.Count == 0
        ? "No pending migrations."
        : $"Applying {pending.Count} migration(s): {string.Join(", ", pending)}");
    await db.Database.MigrateAsync();
    Console.WriteLine($"Done. Last migration: {(await db.Database.GetAppliedMigrationsAsync()).LastOrDefault()}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Migration failed: {ex.GetType().Name}: {ex.Message}");
    return 1;
}
