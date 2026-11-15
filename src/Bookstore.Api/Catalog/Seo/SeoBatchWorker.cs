using Microsoft.Extensions.Options;

namespace Bookstore.Api.Catalog.Seo;

public sealed class SeoBatchOptions
{
    public const string SectionName = "SeoBatch";

    public bool Enabled { get; set; }

    // Most batches finish within an hour; there is no reason to ask every few seconds.
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMinutes(5);
}

public sealed class SeoBatchWorker(IServiceScopeFactory scopes, IOptions<SeoBatchOptions> options, ILogger<SeoBatchWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        using PeriodicTimer timer = new(options.Value.PollInterval);
        do
        {
            try
            {
                using IServiceScope scope = scopes.CreateScope();
                SeoBatchProcessor processor = scope.ServiceProvider.GetRequiredService<SeoBatchProcessor>();
                ISeoBatchJobStore jobs = scope.ServiceProvider.GetRequiredService<ISeoBatchJobStore>();

                foreach (string batchId in jobs.Pending())
                {
                    await processor.CollectAsync(batchId, stoppingToken);
                }

                await processor.SubmitMissingAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "SEO batch run failed; the next tick tries again");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
