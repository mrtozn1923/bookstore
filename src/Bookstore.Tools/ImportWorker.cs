using Microsoft.Extensions.Options;

namespace Bookstore.Tools;

public sealed class ImportWorker(
    IOptions<ImportOptions> options,
    ILogger<ImportWorker> logger,
    IHostApplicationLifetime lifetime) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ImportOptions settings = options.Value;
        int batch = 0;
        try
        {
            string[] lines = await File.ReadAllLinesAsync(settings.File, stoppingToken);
            List<BookRow> rows = [.. lines.Skip(1).Where(l => l.Length > 0).Select(BookRow.Parse)];
            List<BookRow> valid = [.. rows.Where(r => r.HasValidIsbn13())];

            foreach (BookRow invalid in rows.Except(valid))
            {
                logger.LogWarning("Skipping {Title}: invalid ISBN-13 {Isbn}", invalid.Title, invalid.Isbn);
            }

            BookRow[][] batches = [.. valid.Chunk(settings.BatchSize)];
            logger.LogInformation("Importing {Count} books from {File} in {Batches} batches", valid.Count, settings.File, batches.Length);

            foreach (BookRow[] chunk in batches)
            {
                batch++;
                await Task.Delay(settings.DelayMs, stoppingToken);
                logger.LogInformation("Batch {Batch}/{Total} imported: {Titles}", batch, batches.Length, string.Join(", ", chunk.Select(b => b.Title)));
            }

            logger.LogInformation("Import finished: {Count} books in {Batches} batches", valid.Count, batches.Length);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Import stopped during batch {Batch}; next run resumes from batch {Batch}", batch, batch);
        }
        catch (IOException ex)
        {
            logger.LogError("Cannot read import file: {Message}", ex.Message);
            Environment.ExitCode = 1;
        }
        finally
        {
            lifetime.StopApplication();
        }
    }
}
