namespace Bookstore.FileDrops;

// A drop folder on the local disk: for tests and for partners that share a network folder.
public sealed class LocalFolderFileDrop(string root) : IFileDrop, IFileDropSession
{
    public Task<IFileDropSession> OpenAsync(CancellationToken cancellationToken) => Task.FromResult<IFileDropSession>(this);

    public Task<IReadOnlyList<RemoteFile>> ListAsync(string folder, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RemoteFile>>([.. new DirectoryInfo(Full(folder)).EnumerateFiles()
            .Select(f => new RemoteFile(f.Name, f.Length, f.LastWriteTimeUtc))]);

    public async Task DownloadAsync(string path, Stream destination, CancellationToken cancellationToken)
    {
        await using var source = File.OpenRead(Full(path));
        await source.CopyToAsync(destination, cancellationToken);
    }

    public async Task UploadAsync(string path, Stream content, CancellationToken cancellationToken)
    {
        var temporary = Full(path) + ".part";
        await using (var target = File.Create(temporary))
        {
            await content.CopyToAsync(target, cancellationToken);
        }

        File.Move(temporary, Full(path), overwrite: true);
    }

    public Task MoveAsync(string path, string newPath, CancellationToken cancellationToken)
    {
        File.Move(Full(path), Full(newPath), overwrite: true);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private string Full(string path) => Path.Combine(root, path.TrimStart('/'));
}
