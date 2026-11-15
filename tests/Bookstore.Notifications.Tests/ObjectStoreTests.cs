using Amazon.S3;
using Azure.Storage.Blobs;
using Bookstore.Notifications.Storage;
using Microsoft.Extensions.Options;

namespace Bookstore.Notifications.Tests;

// Same contract against Azurite and an S3-compatible server; skipped unless the environment points at them
public sealed class ObjectStoreTests
{
    private static readonly HttpClient Http = new();

    public static TheoryData<string> Stores => ["azure", "s3"];

    [Theory]
    [MemberData(nameof(Stores))]
    public async Task Signed_urls_download_once_uploaded_and_upload_urls_cannot_read(string kind)
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        (IFileStore files, IDirectUploadStore uploads) = await CreateAsync(kind, ct);
        string path = $"tests/{Guid.NewGuid():N}/invoice.pdf";
        byte[] content = "%PDF-1.7 Dune x2"u8.ToArray();

        await files.SaveAsync(path, new MemoryStream(content), "application/pdf", ct);
        Uri download = await files.GetDownloadUrlAsync(path, TimeSpan.FromMinutes(1), "invoice.pdf", ct);
        Assert.Equal(content, await Http.GetByteArrayAsync(download, ct));

        DirectUpload upload = await uploads.CreateUploadAsync(path + ".jpg", "image/jpeg", TimeSpan.FromMinutes(1), ct);
        using HttpRequestMessage put = new(HttpMethod.Put, upload.Url) { Content = new ByteArrayContent(content) };
        foreach ((string name, string value) in upload.Headers)
        {
            if (!put.Headers.TryAddWithoutValidation(name, value))
            {
                put.Content.Headers.TryAddWithoutValidation(name, value);
            }
        }

        using HttpResponseMessage putResponse = await Http.SendAsync(put, ct);
        Assert.True(putResponse.IsSuccessStatusCode);
        Assert.True(await files.ExistsAsync(path + ".jpg", ct));

        using HttpResponseMessage read = await Http.GetAsync(upload.Url, ct);
        Assert.False(read.IsSuccessStatusCode);

        Assert.True(await files.DeleteAsync(path, ct));
        Assert.True(await files.DeleteAsync(path + ".jpg", ct));
        Assert.False(await files.ExistsAsync(path, ct));
        await Assert.ThrowsAsync<FileNotFoundException>(() => files.OpenReadAsync(path, ct));
    }

    private static async Task<(IFileStore, IDirectUploadStore)> CreateAsync(string kind, CancellationToken ct)
    {
        if (kind == "azure")
        {
            string? connection = Environment.GetEnvironmentVariable("AZURITE_CONNECTION_STRING");
            Assert.SkipWhen(connection is null, "AZURITE_CONNECTION_STRING is not set");
            BlobContainerClient container = new(connection, "bookstore-tests");
            await container.CreateIfNotExistsAsync(cancellationToken: ct);
            AzureBlobFileStore azure = new(container, TimeProvider.System);
            return (azure, azure);
        }

        string? serviceUrl = Environment.GetEnvironmentVariable("S3_SERVICE_URL");
        Assert.SkipWhen(serviceUrl is null, "S3_SERVICE_URL is not set");
        AmazonS3Client client = new(new AmazonS3Config { ServiceURL = serviceUrl, ForcePathStyle = true, AuthenticationRegion = "eu-central-1" });
        try
        {
            await client.PutBucketAsync("bookstore-tests", ct);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode is "BucketAlreadyOwnedByYou" or "BucketAlreadyExists")
        {
        }

        S3FileStore s3 = new(client, Options.Create(new S3FileStoreOptions { BucketName = "bookstore-tests", ServiceUrl = serviceUrl }), TimeProvider.System);
        return (s3, s3);
    }
}
