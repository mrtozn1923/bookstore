namespace Bookstore.IO.Files;

public static class AtomicFile
{
    // Writes to a temp file in the same directory, flushes to disk, then swaps it in.
    public static async Task WriteAllBytesAsync(string path, ReadOnlyMemory<byte> content, CancellationToken ct = default)
    {
        string fullPath = Path.GetFullPath(path);
        string directory = Path.GetDirectoryName(fullPath)!;
        string tempPath = Path.Join(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            var options = new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Share = FileShare.None,
                Options = FileOptions.Asynchronous,
                PreallocationSize = content.Length
            };

            await using (var stream = new FileStream(tempPath, options))
            {
                await stream.WriteAsync(content, ct);
                stream.Flush(flushToDisk: true);
            }

            File.Move(tempPath, fullPath, overwrite: true);
        }
        catch
        {
            File.Delete(tempPath);
            throw;
        }
    }
}
