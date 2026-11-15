using Azure.Storage.Sas;

namespace Bookstore.Api.Uploads;

public sealed class UploadOptions
{
    public long MaxCoverBytes { get; set; } = 10 * 1024 * 1024;
    public long MaxSampleChapterBytes { get; set; } = 20 * 1024 * 1024;
    public long MaxCoverPixels { get; set; } = 40_000_000;
    public int MaxArchiveEntries { get; set; } = 2_000;
    public long MaxExpandedBytes { get; set; } = 50 * 1024 * 1024;
    public long MaxCompressionRatio { get; set; } = 100;
    public string QuarantineContainer { get; set; } = "uploads-quarantine";
    public string ApprovedContainer { get; set; } = "uploads-approved";
    public TimeSpan UploadSasLifetime { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan DownloadSasLifetime { get; set; } = TimeSpan.FromMinutes(5);
    public SasProtocol SasProtocol { get; set; } = SasProtocol.Https;

    public long MaxBytes(UploadPurpose purpose) =>
        purpose is UploadPurpose.Cover ? MaxCoverBytes : MaxSampleChapterBytes;
}

public sealed class ClamAvOptions
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 3310;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
