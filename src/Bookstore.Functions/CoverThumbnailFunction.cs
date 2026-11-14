using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Bookstore.Functions;

public sealed class CoverThumbnailFunction(ILogger<CoverThumbnailFunction> logger)
{
    // Flex Consumption supports only the Event Grid based blob trigger
    [Function(nameof(CoverThumbnail))]
    public async Task CoverThumbnail(
        [BlobTrigger("covers/{name}", Source = BlobTriggerSource.EventGrid, Connection = "CoversStorage")] BlobClient cover,
        [BlobInput("covers-thumbnails", Connection = "CoversStorage")] BlobContainerClient thumbnails,
        string name,
        CancellationToken cancellationToken)
    {
        BlobProperties source = await cover.GetPropertiesAsync(cancellationToken: cancellationToken);
        string sourceETag = source.ETag.ToString();
        BlobClient thumbnail = thumbnails.GetBlobClient(Path.ChangeExtension(name, ".webp"));

        // Event Grid delivers at least once: the same version must not be processed twice
        try
        {
            BlobProperties existing = await thumbnail.GetPropertiesAsync(cancellationToken: cancellationToken);
            if (existing.Metadata.TryGetValue("sourceetag", out string? processed) && processed == sourceETag)
            {
                logger.LogInformation("Thumbnail for {Name} is up to date (ETag {ETag}), skipping", name, sourceETag);
                return;
            }
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
        }

        await using Stream image = await cover.OpenReadAsync(cancellationToken: cancellationToken);
        byte[] webp = Bookstore.Functions.CoverThumbnail.CreateWebp(image);

        await thumbnails.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        await thumbnail.UploadAsync(new BinaryData(webp), new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = "image/webp", CacheControl = "public, max-age=31536000" },
            Metadata = new Dictionary<string, string> { ["sourceetag"] = sourceETag },
        }, cancellationToken);

        logger.LogInformation("Created {Thumbnail} ({Bytes} bytes) from {Name} ({SourceBytes} bytes)",
            thumbnail.Name, webp.Length, name, source.ContentLength);
    }
}
