namespace Bookstore.Notifications;

public sealed record StoredFile(string Path, long Length, string ContentType);

public interface IFileStore
{
    Task<StoredFile> SaveAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default);

    // FileNotFoundException if the path does not exist
    Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default);
}
