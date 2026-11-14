using System.Diagnostics;
using Bookstore.Data;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.MigrationService;

// Applies EF Core migrations, seeds sample books and stops the process
public sealed class MigrationWorker(
    IServiceProvider services,
    IConfiguration configuration,
    IHostApplicationLifetime lifetime,
    ILogger<MigrationWorker> logger) : BackgroundService
{
    public const string ActivitySourceName = "Bookstore.MigrationService";
    private static readonly ActivitySource activitySource = new(ActivitySourceName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using Activity? activity = activitySource.StartActivity("Migrate BookstoreDb");

        try
        {
            await using AsyncServiceScope scope = services.CreateAsyncScope();
            BookstoreContext db = scope.ServiceProvider.GetRequiredService<BookstoreContext>();

            string[] pending = [.. await db.Database.GetPendingMigrationsAsync(stoppingToken)];
            await db.Database.MigrateAsync(stoppingToken);
            logger.LogInformation("Applied {Count} migration(s)", pending.Length);

            int added = await SampleBooks.SeedAsync(db, configuration.GetValue<int>("Seed:GeneratedBooks"), stoppingToken);
            logger.LogInformation("Seeded {Added} book(s)", added);
        }
        catch (Exception ex)
        {
            // An unhandled exception in a BackgroundService still ends the process with exit code 0
            activity?.AddException(ex);
            logger.LogError(ex, "Database migration failed");
            Environment.ExitCode = 1;
        }
        finally
        {
            lifetime.StopApplication();
        }
    }
}
