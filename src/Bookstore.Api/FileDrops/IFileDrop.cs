namespace Bookstore.FileDrops;

public sealed record RemoteFile(string Name, long Size, DateTimeOffset LastWriteUtc);

// Port: a folder on someone else's server (SFTP, FTP) or, in tests, on the local disk.
public interface IFileDrop
{
    Task<IFileDropSession> OpenAsync(CancellationToken cancellationToken);
}

public interface IFileDropSession : IAsyncDisposable
{
    Task<IReadOnlyList<RemoteFile>> ListAsync(string folder, CancellationToken cancellationToken);

    Task DownloadAsync(string path, Stream destination, CancellationToken cancellationToken);

    // Uploads under a temporary name, then renames: the other side never sees a half-written file.
    Task UploadAsync(string path, Stream content, CancellationToken cancellationToken);

    Task MoveAsync(string path, string newPath, CancellationToken cancellationToken);
}

public sealed class FileDropException(string message, Exception? innerException = null) : Exception(message, innerException);
