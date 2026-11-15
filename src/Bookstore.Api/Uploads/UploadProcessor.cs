using System.Net.Sockets;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Uploads;

public sealed class UploadProcessor(
    UploadQueue queue, UploadStore store, BlobServiceClient blobs, EpubInspector epub, CoverReencoder reencoder,
    ClamAvScanner scanner, IOptions<UploadOptions> options, ILogger<UploadProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (Guid id in queue.Reader.ReadAllAsync(stoppingToken))
        {
            if (store.Get(id) is not { } record)
            {
                continue;
            }

            try
            {
                await ProcessAsync(record, stoppingToken);
            }
            catch (InvalidUploadException ex)
            {
                (record.Status, record.Reason) = (UploadStatus.Rejected, ex.Message);
                await Quarantine.GetBlobClient(record.QuarantineBlob).DeleteIfExistsAsync(cancellationToken: stoppingToken);
            }
            catch (Exception ex) when (ex is ScannerUnavailableException or SocketException or IOException or OperationCanceledException
                                       && !stoppingToken.IsCancellationRequested)
            {
                // Fail closed: the file stays in quarantine and is never served
                (record.Status, record.Reason) = (UploadStatus.ScanFailed, ex.GetType().Name);
            }

            logger.LogInformation("Upload {UploadId} ({DisplayName}) finished as {Status}: {Reason}",
                record.Id, record.DisplayName, record.Status, record.Reason);
        }
    }

    private BlobContainerClient Quarantine => blobs.GetBlobContainerClient(options.Value.QuarantineContainer);

    private async Task ProcessAsync(UploadRecord record, CancellationToken ct)
    {
        BlobClient quarantined = Quarantine.GetBlobClient(record.QuarantineBlob);
        BlobProperties properties = await quarantined.GetPropertiesAsync(cancellationToken: ct);

        // The SAS cannot limit the size, so the first check is here
        long limit = options.Value.MaxBytes(record.Purpose);
        if (properties.ContentLength > limit)
        {
            throw new InvalidUploadException($"{properties.ContentLength:N0} bytes, the limit is {limit:N0}.");
        }

        using MemoryStream buffer = new((int)properties.ContentLength);
        await quarantined.DownloadToAsync(buffer, ct);
        byte[] content = buffer.ToArray();

        FileFormat format = FileSignatures.Detect(content) ?? throw new InvalidUploadException("Unknown file format.");
        if (!FileSignatures.IsAllowed(record.Purpose, format))
        {
            throw new InvalidUploadException($"{format} is not allowed for {record.Purpose}.");
        }

        if (format is FileFormat.Epub && await epub.FindProblemAsync(new MemoryStream(content), ct) is { } problem)
        {
            throw new InvalidUploadException(problem);
        }

        ScanResult scan = await scanner.ScanAsync(new MemoryStream(content), ct);
        if (!scan.IsClean)
        {
            (record.Status, record.Reason) = (UploadStatus.Infected, scan.Signature);
            await quarantined.DeleteAsync(cancellationToken: ct);
            return;
        }

        byte[] approved = record.Purpose is UploadPurpose.Cover ? reencoder.Reencode(content) : content;
        string name = $"{record.Id:N}.{(record.Purpose is UploadPurpose.Cover ? "webp" : format.ToString().ToLowerInvariant())}";
        await blobs.GetBlobContainerClient(options.Value.ApprovedContainer).GetBlobClient(name)
            .UploadAsync(new BinaryData(approved), overwrite: false, ct);
        await quarantined.DeleteAsync(cancellationToken: ct);

        (record.ApprovedBlob, record.Format, record.Status) = (name, record.Purpose is UploadPurpose.Cover ? FileFormat.WebP : format, UploadStatus.Approved);
    }
}
