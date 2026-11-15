using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Bookstore.Notifications.Storage;

public sealed class LocalFileStoreOptions
{
    [Required]
    public string RootPath { get; set; } = "";

    // Base address of the API that serves /files/... downloads
    [Required]
    public Uri PublicBaseUrl { get; set; } = new("http://127.0.0.1:52560/");

    // HMAC key for download URLs; empty means a random key per process (URLs die on restart)
    public string? UrlSigningKey { get; set; }
}

public sealed class LocalFileStore(IOptions<LocalFileStoreOptions> options, TimeProvider time) : IFileStore
{
    private readonly string _root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.Value.RootPath)) + Path.DirectorySeparatorChar;
    private readonly byte[] _signingKey = string.IsNullOrEmpty(options.Value.UrlSigningKey)
        ? RandomNumberGenerator.GetBytes(32)
        : Encoding.UTF8.GetBytes(options.Value.UrlSigningKey);

    public LocalFileStore(IOptions<LocalFileStoreOptions> options) : this(options, TimeProvider.System)
    {
    }

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

    // The disk cannot sign URLs, so the API does: /files/{path}?expires=...&name=...&sig=HMAC
    public Task<Uri> GetDownloadUrlAsync(string path, TimeSpan lifetime, string? downloadFileName = null, CancellationToken cancellationToken = default)
    {
        StoragePath.Validate(path);
        long expires = time.GetUtcNow().Add(lifetime).ToUnixTimeSeconds();
        string name = downloadFileName ?? Path.GetFileName(path);
        string query = $"expires={expires}&name={Uri.EscapeDataString(name)}&sig={Sign(path, expires, name)}";
        return Task.FromResult(new Uri(options.Value.PublicBaseUrl, $"files/{path}?{query}"));
    }

    public bool VerifyDownload(string path, long expires, string name, string signature) =>
        time.GetUtcNow().ToUnixTimeSeconds() <= expires
        && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Sign(path, expires, name)), Encoding.ASCII.GetBytes(signature));

    private string Sign(string path, long expires, string name) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(_signingKey, Encoding.UTF8.GetBytes($"{path}\n{expires}\n{name}")));

    private string Resolve(string path)
    {
        StoragePath.Validate(path);

        // Second line of defense: whatever the string looked like, the final path must stay under the root
        string fullPath = Path.GetFullPath(Path.Combine(_root, path));
        if (!fullPath.StartsWith(_root, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Path '{path}' is outside the file store root.", nameof(path));
        }

        return fullPath;
    }
}
