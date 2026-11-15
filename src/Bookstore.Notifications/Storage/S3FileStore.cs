using System.ComponentModel.DataAnnotations;
using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace Bookstore.Notifications.Storage;

public sealed class S3FileStoreOptions
{
    [Required]
    public string BucketName { get; set; } = "";

    // Empty for AWS; the endpoint of an S3-compatible server otherwise
    public string? ServiceUrl { get; set; }

    public string Region { get; set; } = "eu-central-1";
}

public sealed class S3FileStore(IAmazonS3 s3, IOptions<S3FileStoreOptions> options, TimeProvider time) : IFileStore, IDirectUploadStore
{
    private string Bucket => options.Value.BucketName;

    // Presigned URLs are https unless told otherwise, even when ServiceUrl is http
    private Protocol UrlProtocol => options.Value.ServiceUrl?.StartsWith("http://", StringComparison.OrdinalIgnoreCase) == true ? Protocol.HTTP : Protocol.HTTPS;

    public async Task<StoredFile> SaveAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = Bucket,
            Key = StoragePath.Validate(path),
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
        }, cancellationToken);

        GetObjectMetadataResponse metadata = await s3.GetObjectMetadataAsync(Bucket, path, cancellationToken);
        return new StoredFile(path, metadata.ContentLength, contentType);
    }

    public async Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            GetObjectResponse response = await s3.GetObjectAsync(Bucket, StoragePath.Validate(path), cancellationToken);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new FileNotFoundException($"No stored file at '{path}'.", ex);
        }
    }

    public async Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        // S3 answers 204 whether or not the object existed
        if (!await ExistsAsync(path, cancellationToken))
        {
            return false;
        }

        await s3.DeleteObjectAsync(Bucket, path, cancellationToken);
        return true;
    }

    public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            await s3.GetObjectMetadataAsync(Bucket, StoragePath.Validate(path), cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<Uri> GetDownloadUrlAsync(string path, TimeSpan lifetime, string? downloadFileName = null, CancellationToken cancellationToken = default)
    {
        GetPreSignedUrlRequest request = new()
        {
            BucketName = Bucket,
            Key = StoragePath.Validate(path),
            Verb = HttpVerb.GET,
            Expires = time.GetUtcNow().Add(lifetime).UtcDateTime,
            Protocol = UrlProtocol,
        };
        request.ResponseHeaderOverrides.ContentDisposition = StoragePath.ContentDisposition(downloadFileName ?? Path.GetFileName(path));
        return new Uri(await s3.GetPreSignedURLAsync(request));
    }

    public async Task<DirectUpload> CreateUploadAsync(string path, string contentType, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        DateTimeOffset expires = time.GetUtcNow().Add(lifetime);
        string url = await s3.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = Bucket,
            Key = StoragePath.Validate(path),
            Verb = HttpVerb.PUT,
            ContentType = contentType,
            Expires = expires.UtcDateTime,
            Protocol = UrlProtocol,
        });
        return new DirectUpload(new Uri(url), "PUT", new Dictionary<string, string> { ["Content-Type"] = contentType }, expires);
    }
}
