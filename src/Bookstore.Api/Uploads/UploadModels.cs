using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Bookstore.Api.Uploads;

public enum UploadPurpose { Cover, SampleChapter }

public enum FileFormat { Jpeg, Png, WebP, Pdf, Epub }

public enum UploadStatus { AwaitingUpload, Processing, Approved, Rejected, Infected, ScanFailed }

public sealed class UploadRecord
{
    public required Guid Id { get; init; }
    public required string PublisherId { get; init; }
    public required string Isbn { get; init; }
    public required UploadPurpose Purpose { get; init; }
    public required string DisplayName { get; init; }
    public string QuarantineBlob => $"{Id:N}";
    public UploadStatus Status { get; set; } = UploadStatus.AwaitingUpload;
    public FileFormat? Format { get; set; }
    public string? ApprovedBlob { get; set; }
    public string? Reason { get; set; }
}

public sealed record CreateUploadRequest(string Isbn, UploadPurpose Purpose, string FileName, long Length);

public sealed record CreateUploadResponse(Guid Id, Uri UploadUrl);

public sealed record UploadStatusResponse(Guid Id, UploadStatus Status, string? Reason);

public sealed class InvalidUploadException(string message) : Exception(message);

public sealed class ScannerUnavailableException(string message) : Exception(message);

// Stands in for a database table in this post
public sealed class UploadStore
{
    private readonly ConcurrentDictionary<Guid, UploadRecord> records = new();

    public UploadRecord Add(UploadRecord record) => records[record.Id] = record;

    public UploadRecord? Get(Guid id) => records.GetValueOrDefault(id);
}

public sealed class UploadQueue
{
    private readonly Channel<Guid> channel = Channel.CreateUnbounded<Guid>();

    public ChannelReader<Guid> Reader => channel.Reader;

    public ValueTask EnqueueAsync(Guid id, CancellationToken ct) => channel.Writer.WriteAsync(id, ct);
}
