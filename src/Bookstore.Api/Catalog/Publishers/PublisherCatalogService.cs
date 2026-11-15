using System.Collections.Concurrent;
using System.Net.Http.Headers;
using Anthropic;
using Anthropic.Core;
using Anthropic.Models.Files;
using Anthropic.Models.Messages;
using Bookstore.Api.Ai;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Catalog.Publishers;

public sealed record PublisherCatalogFile(string FileId, string FileName, DateTimeOffset? ExpiresAt);

public sealed record CatalogAnswer(string Text, ReplyOutcome Outcome, TokenUsage Usage);

// file_id values stay on the server: anyone with a key for the workspace can read any file in it.
public interface IPublisherCatalogStore
{
    PublisherCatalogFile? Find(string publisherId);
    void Save(string publisherId, PublisherCatalogFile file);
}

public sealed class InMemoryPublisherCatalogStore : IPublisherCatalogStore
{
    private readonly ConcurrentDictionary<string, PublisherCatalogFile> _files = new();

    public PublisherCatalogFile? Find(string publisherId) => _files.TryGetValue(publisherId, out PublisherCatalogFile? f) ? f : null;
    public void Save(string publisherId, PublisherCatalogFile file) => _files[publisherId] = file;
}

public sealed class PublisherCatalogService(
    IAnthropicClient claude,
    IPublisherCatalogStore store,
    IOptions<ClaudeOptions> options,
    ILogger<PublisherCatalogService> logger)
{
    // Catalogs change every season; the file deletes itself after 30 days (the API accepts 1 hour to 90 days).
    public static readonly TimeSpan Retention = TimeSpan.FromDays(30);

    public async Task<PublisherCatalogFile> UploadAsync(
        string publisherId, Stream pdf, string fileName, CancellationToken cancellationToken)
    {
        FileMetadata uploaded = await claude.Files.Upload(new FileUploadParams
        {
            File = new BinaryContent
            {
                Stream = pdf,
                FileName = fileName,
                ContentType = new MediaTypeHeaderValue("application/pdf"),
            },
            ExpiresInSeconds = (long)Retention.TotalSeconds,
        }, cancellationToken);

        PublisherCatalogFile file = new(uploaded.ID, uploaded.Filename, uploaded.ExpiresAt);
        store.Save(publisherId, file);
        logger.LogInformation("Publisher {PublisherId} catalog uploaded as {FileId} ({Bytes} bytes)",
            publisherId, uploaded.ID, uploaded.SizeBytes);
        return file;
    }

    public static MessageCreateParams BuildRequest(string fileId, string question, ClaudeOptions settings) => new()
    {
        Model = settings.Model,
        MaxTokens = 2048,
        System = "You answer questions about a publisher's catalog for Bookstore's buyers. Use only the catalog. Say so when the catalog does not answer the question.",
        Messages =
        [
            new UserMessageParam
            {
                Content = new List<ContentBlockParam>
                {
                    // The same document in every question: cache it, the file is billed as input tokens each time.
                    new DocumentBlockParam
                    {
                        Source = new FileDocumentSource { FileID = fileId },
                        Title = "Publisher catalog",
                        CacheControl = new CacheControlEphemeral(),
                    },
                    new TextBlockParam { Text = question },
                },
            },
        ],
    };

    // Returns null when the publisher has no catalog file or it has expired.
    public async Task<CatalogAnswer?> AskAsync(string publisherId, string question, CancellationToken cancellationToken)
    {
        PublisherCatalogFile? file = store.Find(publisherId);
        if (file is null || file.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        Message message = await claude.Messages.Create(BuildRequest(file.FileId, question, options.Value), cancellationToken);
        ClaudeReply reply = ClaudeReplyReader.Read(message);
        return new CatalogAnswer(reply.Text, reply.Outcome, reply.Usage);
    }
}
