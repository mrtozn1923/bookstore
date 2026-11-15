namespace Bookstore.Api.FileStorage;

// The bytes live in the file store; what we know about them lives in the database
public sealed class StoredFileRecord
{
    public required string Path { get; init; }
    public required string Purpose { get; init; }
    public required string ContentType { get; init; }
    public long Length { get; init; }
    public required string Sha256 { get; init; }

    // Who may get a download URL, e.g. the customer of the order
    public string? OwnerId { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
