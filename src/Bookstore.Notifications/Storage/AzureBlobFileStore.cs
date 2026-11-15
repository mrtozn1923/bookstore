using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;

namespace Bookstore.Notifications.Storage;

public sealed class AzureBlobFileStore(BlobContainerClient container, TimeProvider time) : IFileStore, IDirectUploadStore
{
    public async Task<StoredFile> SaveAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        BlobClient blob = container.GetBlobClient(StoragePath.Validate(path));
        await blob.UploadAsync(content, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } }, cancellationToken);
        BlobProperties properties = await blob.GetPropertiesAsync(cancellationToken: cancellationToken);
        return new StoredFile(path, properties.ContentLength, contentType);
    }

    public async Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            BlobDownloadStreamingResult result = await container.GetBlobClient(StoragePath.Validate(path)).DownloadStreamingAsync(cancellationToken: cancellationToken);
            return result.Content;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new FileNotFoundException($"No stored file at '{path}'.", ex);
        }
    }

    public async Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default) =>
        await container.GetBlobClient(StoragePath.Validate(path)).DeleteIfExistsAsync(cancellationToken: cancellationToken);

    public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default) =>
        await container.GetBlobClient(StoragePath.Validate(path)).ExistsAsync(cancellationToken);

    public Task<Uri> GetDownloadUrlAsync(string path, TimeSpan lifetime, string? downloadFileName = null, CancellationToken cancellationToken = default)
    {
        BlobSasBuilder sas = NewSas(path, lifetime, BlobSasPermissions.Read);
        sas.ContentDisposition = StoragePath.ContentDisposition(downloadFileName ?? Path.GetFileName(path));
        return SignAsync(path, sas, cancellationToken);
    }

    public async Task<DirectUpload> CreateUploadAsync(string path, string contentType, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        // Create only: the URL cannot read, list or overwrite an existing blob
        BlobSasBuilder sas = NewSas(path, lifetime, BlobSasPermissions.Create);
        Uri url = await SignAsync(path, sas, cancellationToken);
        return new DirectUpload(url, "PUT", new Dictionary<string, string> { ["x-ms-blob-type"] = "BlockBlob", ["Content-Type"] = contentType }, sas.ExpiresOn);
    }

    private BlobSasBuilder NewSas(string path, TimeSpan lifetime, BlobSasPermissions permissions)
    {
        DateTimeOffset now = time.GetUtcNow();
        BlobSasBuilder sas = new(permissions, now + lifetime)
        {
            BlobContainerName = container.Name,
            BlobName = StoragePath.Validate(path),
            Resource = "b",
            // A few minutes of tolerance for clocks that run behind
            StartsOn = now.AddMinutes(-5),
        };
        return sas;
    }

    private async Task<Uri> SignAsync(string path, BlobSasBuilder sas, CancellationToken cancellationToken)
    {
        BlobClient blob = container.GetBlobClient(path);
        if (blob.CanGenerateSasUri)
        {
            // Client built with the account key (Azurite, or a key from Key Vault)
            return blob.GenerateSasUri(sas);
        }

        // Managed identity / Entra ID: a user delegation SAS signed with a key the service hands out
        BlobServiceClient service = container.GetParentBlobServiceClient();
        UserDelegationKey key = await service.GetUserDelegationKeyAsync(sas.StartsOn, sas.ExpiresOn, cancellationToken);
        BlobUriBuilder builder = new(blob.Uri) { Sas = sas.ToSasQueryParameters(key, service.AccountName) };
        return builder.ToUri();
    }
}
