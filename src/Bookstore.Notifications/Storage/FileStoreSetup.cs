using Amazon.S3;
using Amazon.S3.Util;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bookstore.Notifications.Storage;

public static class FileStoreSetup
{
    // Development convenience: create the container/bucket; in production infrastructure code owns this
    public static async Task EnsureFileStoreAsync(this IServiceProvider services, string[] corsOrigins, CancellationToken cancellationToken = default)
    {
        if (services.GetService<BlobContainerClient>() is { } container)
        {
            // Private container: no anonymous access, every read goes through a SAS
            await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

            // The browser PUTs straight to blob storage, so the storage account needs a CORS rule
            BlobServiceClient service = container.GetParentBlobServiceClient();
            BlobServiceProperties properties = await service.GetPropertiesAsync(cancellationToken);
            properties.Cors = [new BlobCorsRule
            {
                AllowedOrigins = string.Join(',', corsOrigins),
                AllowedMethods = "PUT,GET",
                AllowedHeaders = "content-type,x-ms-blob-type",
                ExposedHeaders = "etag",
                MaxAgeInSeconds = 600,
            }];
            await service.SetPropertiesAsync(properties, cancellationToken);
        }

        if (services.GetService<IAmazonS3>() is { } s3)
        {
            string bucket = services.GetRequiredService<IOptions<S3FileStoreOptions>>().Value.BucketName;
            if (!await AmazonS3Util.DoesS3BucketExistV2Async(s3, bucket))
            {
                await s3.PutBucketAsync(bucket, cancellationToken);
            }
        }
    }
}
