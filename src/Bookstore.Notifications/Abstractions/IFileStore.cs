namespace Bookstore.Notifications;

public sealed record StoredFile(string Path, long Length, string ContentType);

// Paths are relative, '/'-separated keys such as "invoices/2026/BKS-2026-001042.pdf", on every backend
public interface IFileStore
{
    Task<StoredFile> SaveAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default);

    // FileNotFoundException if the path does not exist
    Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);

    // A short-lived URL the client downloads from directly; the API is not in the data path
    Task<Uri> GetDownloadUrlAsync(string path, TimeSpan lifetime, string? downloadFileName = null, CancellationToken cancellationToken = default);
}

public sealed record DirectUpload(Uri Url, string Method, IReadOnlyDictionary<string, string> Headers, DateTimeOffset ExpiresAt);

// Object stores can hand out a URL the browser uploads to; the local disk cannot
public interface IDirectUploadStore
{
    Task<DirectUpload> CreateUploadAsync(string path, string contentType, TimeSpan lifetime, CancellationToken cancellationToken = default);
}

public static class StoragePath
{
    // Rejects anything that is not a plain relative key before it reaches a file system or a URL
    public static string Validate(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length > 512 || path.StartsWith('/') || path.Contains('\\') || path.Contains(':')
            || path.Any(char.IsControl) || path.Split('/').Any(segment => segment is "" or "." or ".."))
        {
            throw new ArgumentException($"'{path}' is not a valid storage path.", nameof(path));
        }

        return path;
    }

    public static string ContentDisposition(string fileName) =>
        new System.Net.Mime.ContentDisposition { FileName = fileName, DispositionType = "attachment" }.ToString();
}
