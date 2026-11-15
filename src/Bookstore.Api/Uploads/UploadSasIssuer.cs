using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Uploads;

public sealed class UploadSasIssuer(BlobServiceClient blobs, IOptions<UploadOptions> options, TimeProvider time)
{
    // One blob, create only, a few minutes, in the quarantine container
    public Uri ForUpload(UploadRecord record)
    {
        UploadOptions o = options.Value;
        BlobClient blob = blobs.GetBlobContainerClient(o.QuarantineContainer).GetBlobClient(record.QuarantineBlob);
        BlobSasBuilder sas = new(BlobSasPermissions.Create, time.GetUtcNow().Add(o.UploadSasLifetime))
        {
            BlobContainerName = blob.BlobContainerName,
            BlobName = blob.Name,
            Resource = "b",
            Protocol = o.SasProtocol,
        };
        return blob.GenerateSasUri(sas);
    }

    // Read only, and the response headers are fixed by us, not by the stored blob
    public Uri ForDownload(UploadRecord record)
    {
        UploadOptions o = options.Value;
        BlobClient blob = blobs.GetBlobContainerClient(o.ApprovedContainer).GetBlobClient(record.ApprovedBlob);
        (string contentType, string extension) = record.Format switch
        {
            FileFormat.Pdf => ("application/pdf", "pdf"),
            FileFormat.Epub => ("application/epub+zip", "epub"),
            _ => ("image/webp", "webp"),
        };
        BlobSasBuilder sas = new(BlobSasPermissions.Read, time.GetUtcNow().Add(o.DownloadSasLifetime))
        {
            BlobContainerName = blob.BlobContainerName,
            BlobName = blob.Name,
            Resource = "b",
            Protocol = o.SasProtocol,
            ContentType = contentType,
            ContentDisposition = $"attachment; filename=\"{record.Isbn}-{record.Purpose.ToString().ToLowerInvariant()}.{extension}\"",
        };
        return blob.GenerateSasUri(sas);
    }
}
