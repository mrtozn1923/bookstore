using Microsoft.EntityFrameworkCore;

namespace Bookstore.Data;

public static class DatabaseExtensions
{
    // Development convenience: creates the SQLite database (schema + seed) when the API starts.
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        BookstoreContext db = scope.ServiceProvider.GetRequiredService<BookstoreContext>();
        await db.Database.EnsureCreatedAsync();
    }
}
