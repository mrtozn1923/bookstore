using Bookstore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Data;

public static class DatabaseExtensions
{
    // Development convenience for the mobile series: SQLite files are created (schema + seed) when the API starts.
    // The SQL Server migrations of the reference app are not used with SQLite.
    public static async Task CreateDatabasesAsync(this WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<BookstoreContext>().Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService<AuthContext>().Database.EnsureCreatedAsync();
    }
}
