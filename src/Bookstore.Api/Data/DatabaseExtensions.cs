using Bookstore.Ordering;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Data;

public static class DatabaseExtensions
{
    // Development convenience: applies pending migrations (schema + seed) when the API starts.
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        BookstoreContext db = scope.ServiceProvider.GetRequiredService<BookstoreContext>();
        if (db.Database.IsSqlite())
        {
            // The migrations are written for SQL Server; SQLite gets its schema straight from the model.
            await db.Database.EnsureCreatedAsync();
            return;
        }
        await db.Database.MigrateAsync();
    }

    // Development convenience: demo orders for the back office screens ("Database:DemoOrders": 10000).
    public static async Task SeedDemoOrdersAsync(this WebApplication app, int count)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        BookstoreContext db = scope.ServiceProvider.GetRequiredService<BookstoreContext>();
        await DemoOrders.SeedAsync(db, count, scope.ServiceProvider.GetRequiredService<TimeProvider>());
    }
}
