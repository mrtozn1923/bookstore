using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Bookstore.Notifications.Storage;

public sealed class LocalFileStoreOptions
{
    [Required]
    public string RootPath { get; set; } = "";
}

public sealed class LocalFileStore(IOptions<LocalFileStoreOptions> options) : IFileStore
{
    private readonly string _root = Path.GetFullPath(options.Value.RootPath);

    public async Task<StoredFile> SaveAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        string fullPath = Resolve(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        // Write to a temp file first: a reader never sees a half-written document
        string tempPath = fullPath + ".tmp-" + Guid.NewGuid().ToString("N");
        await using (FileStream file = new(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            await content.CopyToAsync(file, cancellationToken);
        }

        File.Move(tempPath, fullPath, overwrite: true);
        return new StoredFile(path, new FileInfo(fullPath).Length, contentType);
    }

    public Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        string fullPath = Resolve(path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"No stored file at '{path}'.");
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult(stream);
    }

    public Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        string fullPath = Resolve(path);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult(false);
        }

        File.Delete(fullPath);
        return Task.FromResult(true);
    }

    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default) =>
        Task.FromResult(File.Exists(Resolve(path)));

    // The path comes from our own code, but still never leave the root (post 08 covers this in depth)
    private string Resolve(string path)
    {
        string fullPath = Path.GetFullPath(Path.Combine(_root, path));
        if (!fullPath.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Path '{path}' is outside the file store root.", nameof(path));
        }

        return fullPath;
    }
}
