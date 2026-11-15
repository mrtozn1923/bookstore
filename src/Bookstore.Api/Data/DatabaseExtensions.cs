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
}
