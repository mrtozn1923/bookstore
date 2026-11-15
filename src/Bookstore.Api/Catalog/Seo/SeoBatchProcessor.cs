using Anthropic;
using Anthropic.Models.Messages;
using Anthropic.Models.Messages.Batches;

namespace Bookstore.Api.Catalog.Seo;

public sealed record SeoBatchReport(string BatchId, int Saved, IReadOnlyList<string> Retry, IReadOnlyList<string> Rejected);

public sealed class SeoBatchProcessor(
    IAnthropicClient claude,
    IBookRepository books,
    IBookSeoStore seo,
    ISeoBatchJobStore jobs,
    ILogger<SeoBatchProcessor> logger)
{
    // Sends a batch only for books that still have no description and only when nothing is pending.
    public async Task<IReadOnlyList<string>> SubmitMissingAsync(CancellationToken cancellationToken)
    {
        if (jobs.Pending().Count > 0)
        {
            return [];
        }

        List<Book> missing = books.All().Where(b => !seo.HasDescription(b.Isbn)).ToList();
        List<string> created = [];
        foreach (BatchCreateParams batch in BookSeoBatch.Split(missing))
        {
            MessageBatch createdBatch = await claude.Messages.Batches.Create(batch, cancellationToken);
            // Store the id before anything else; the next run polls this batch instead of sending a new one.
            jobs.Add(createdBatch.ID);
            created.Add(createdBatch.ID);
            logger.LogInformation("SEO batch {BatchId} created with {Count} requests", createdBatch.ID, batch.Requests.Count);
        }

        return created;
    }

    // Returns null while the batch is still running.
    public async Task<SeoBatchReport?> CollectAsync(string batchId, CancellationToken cancellationToken)
    {
        MessageBatch batch = await claude.Messages.Batches.Retrieve(batchId, cancellationToken: cancellationToken);
        if (batch.ProcessingStatus.Value() != ProcessingStatus.Ended)
        {
            return null;
        }

        int saved = 0;
        List<string> retry = [];
        List<string> rejected = [];

        await foreach (MessageBatchIndividualResponse item in claude.Messages.Batches.ResultsStreaming(batchId, cancellationToken: cancellationToken))
        {
            string? isbn = BookSeoBatch.IsbnFrom(item.CustomID);
            if (isbn is null)
            {
                logger.LogWarning("SEO batch {BatchId}: unknown custom_id {CustomId}", batchId, item.CustomID);
                continue;
            }

            if (item.Result.TryPickSucceeded(out MessageBatchSucceededResult? succeeded))
            {
                if (TryReadDescription(succeeded.Message, out string description))
                {
                    seo.Save(isbn, description);
                    saved++;
                }
                else
                {
                    rejected.Add(isbn);
                }
            }
            else if (item.Result.TryPickErrored(out MessageBatchErroredResult? errored)
                     && errored.Error.Error.Type.Raw() == "invalid_request_error")
            {
                // The request itself is wrong; sending it again gives the same error.
                logger.LogError("SEO batch {BatchId}: invalid request for {Isbn}", batchId, isbn);
                rejected.Add(isbn);
            }
            else
            {
                // Server errors, expired and canceled requests were not billed and can go into the next batch.
                retry.Add(isbn);
            }
        }

        jobs.Complete(batchId);
        logger.LogInformation("SEO batch {BatchId}: saved={Saved} retry={Retry} rejected={Rejected}",
            batchId, saved, retry.Count, rejected.Count);
        return new SeoBatchReport(batchId, saved, retry, rejected);
    }

    private static bool TryReadDescription(Message message, out string description)
    {
        description = string.Concat(message.Content.Select(b => b.TryPickText(out TextBlock? t) ? t.Text : string.Empty)).Trim();
        // A cut-off answer or a refusal is not a description.
        return message.StopReason?.Value() == StopReason.EndTurn && description.Length is > 0 and <= 300;
    }
}
