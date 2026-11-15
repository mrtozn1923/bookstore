using Bookstore.Inventory.SupplierFeed;
using Bookstore.Payments.BankStatements;
using Microsoft.Extensions.Options;

namespace Bookstore.FileDrops;

public sealed class FileDropPollingOptions
{
    public bool Enabled { get; set; }
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(5);
}

// Polling instead of FileSystemWatcher: the folders are on other servers, and a missed tick is simply caught up by the next one.
public sealed class FileDropPollingJob(
    IServiceScopeFactory scopes,
    IOptions<FileDropPollingOptions> options,
    TimeProvider time,
    ILogger<FileDropPollingJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(options.Value.Interval, time);
        do
        {
            using var scope = scopes.CreateScope();
            try
            {
                await scope.ServiceProvider.GetRequiredService<SupplierFeedProcessor>().RunAsync(stoppingToken);
                await scope.ServiceProvider.GetRequiredService<BankStatementReconciler>().RunAsync(stoppingToken);
            }
            catch (FileDropException ex)
            {
                // The next tick is the retry; an alert fires if this repeats (see "In production").
                logger.LogWarning(ex, "File drop run failed, trying again in {Interval}", options.Value.Interval);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
