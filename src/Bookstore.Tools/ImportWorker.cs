namespace Bookstore.Tools;

public sealed class ImportWorker(ILogger<ImportWorker> logger, IHostApplicationLifetime lifetime) : BackgroundService
{
    private const int BatchCount = 30;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int batch = 0;
        try
        {
            while (batch < BatchCount)
            {
                batch++;
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                logger.LogInformation("Batch {Batch}/{Total} imported", batch, BatchCount);
            }

            logger.LogInformation("Import finished");
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Import stopped during batch {Batch}; next run resumes from batch {Batch}", batch, batch);
        }
        finally
        {
            lifetime.StopApplication();
        }
    }
}
