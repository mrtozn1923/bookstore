using Bookstore.Search;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Microsoft.Extensions.Options;

namespace Bookstore.SearchSync;

public sealed class SearchSyncOptions
{
    public const string SectionName = "SearchSync";

    public int BatchSize { get; set; } = 1_000;

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);
}

// Polls SQL Server for changed books and applies them to the books index with external versions.
public sealed class SearchSyncWorker(
    BookChangeReader reader,
    ElasticsearchClient client,
    IOptions<SearchSyncOptions> options,
    ILogger<SearchSyncWorker> logger) : BackgroundService
{
    private const string CheckpointName = "books";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        SearchSyncOptions settings = options.Value;
        byte[] checkpoint = await reader.GetCheckpointAsync(CheckpointName, stoppingToken);
        logger.LogInformation("Starting from RowVer 0x{Checkpoint}", Convert.ToHexString(checkpoint));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                ChangeBatch batch = await reader.ReadChangesAsync(checkpoint, settings.BatchSize, stoppingToken);
                if (batch.Changes.Count > 0)
                {
                    await ApplyAsync(batch, stoppingToken);
                    // Only after Elasticsearch accepted the batch: a crash before this line replays it.
                    await reader.SaveCheckpointAsync(CheckpointName, batch.LastRowVer, stoppingToken);
                    checkpoint = batch.LastRowVer;
                }

                if (batch.Changes.Count < settings.BatchSize)
                {
                    await Task.Delay(settings.PollInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Sync batch failed; retrying from RowVer 0x{Checkpoint}", Convert.ToHexString(checkpoint));
                await Task.Delay(settings.PollInterval, stoppingToken);
            }
        }
    }

    private async Task ApplyAsync(ChangeBatch batch, CancellationToken cancellationToken)
    {
        BulkRequest request = new(SearchClientFactory.BooksIndex)
        {
            Operations = [.. batch.Changes.Select(ToOperation)]
        };

        BulkResponse response = await client.BulkAsync(request, cancellationToken);
        if (!response.ApiCallDetails.HasSuccessfulStatusCode)
        {
            throw new InvalidOperationException($"Bulk request failed: {response.ApiCallDetails.HttpStatusCode} {response.ElasticsearchServerError?.Error?.Reason}");
        }

        int indexed = 0, deleted = 0, stale = 0;
        foreach (ResponseItem item in response.Items)
        {
            switch (item.Status)
            {
                case 200 or 201 when item.Operation == "delete":
                case 404 when item.Operation == "delete":
                    deleted++;
                    break;
                case 200 or 201:
                    indexed++;
                    break;
                // External version not newer than the one in the index: this change is already there.
                case 409:
                    stale++;
                    break;
                default:
                    throw new InvalidOperationException($"Item {item.Id} failed: {item.Status} {item.Error?.Type} {item.Error?.Reason}");
            }
        }

        logger.LogInformation("Synced {Count} changes ({Indexed} indexed, {Deleted} deleted, {Stale} already up to date) up to RowVer 0x{RowVer}",
            batch.Changes.Count, indexed, deleted, stale, Convert.ToHexString(batch.LastRowVer));
    }

    private static IBulkOperation ToOperation(BookChange change) => change.IsDiscontinued
        ? new BulkDeleteOperation<Book>(change.Book.Isbn) { Version = change.Version, VersionType = VersionType.External }
        : new BulkIndexOperation<Book>(change.Book) { Id = change.Book.Isbn, Version = change.Version, VersionType = VersionType.External };
}
