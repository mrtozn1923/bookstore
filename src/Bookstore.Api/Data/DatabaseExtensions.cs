using Microsoft.EntityFrameworkCore;

namespace Bookstore.Data;

public static class DatabaseExtensions
{
    public static IServiceCollection AddBookstoreDatabase(this IServiceCollection services, string connectionString) =>
        services.AddDbContext<BookstoreContext>(options => options.UseSqlServer(connectionString, sql =>
            // Azure SQL drops connections during failover, scaling and serverless resume: retry those errors
            sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)));

    // Development convenience: applies pending migrations (schema + seed) when the API starts.
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        BookstoreContext db = scope.ServiceProvider.GetRequiredService<BookstoreContext>();
        await db.Database.MigrateAsync();
    }
}
