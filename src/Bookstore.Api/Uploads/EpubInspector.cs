using System.IO.Compression;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Uploads;

public sealed class EpubInspector(IOptions<UploadOptions> options)
{
    private static readonly HashSet<string> NestedArchives = new(StringComparer.OrdinalIgnoreCase) { ".zip", ".epub", ".rar", ".7z", ".gz", ".tar" };

    // Returns null when the archive is acceptable; never extracts anything to disk
    public async Task<string?> FindProblemAsync(Stream epub, CancellationToken ct)
    {
        UploadOptions o = options.Value;
        await using ZipArchive archive = await ZipArchive.CreateAsync(epub, ZipArchiveMode.Read, leaveOpen: true, entryNameEncoding: null, ct);
        if (archive.Entries.Count > o.MaxArchiveEntries)
        {
            return $"{archive.Entries.Count:N0} entries, the limit is {o.MaxArchiveEntries:N0}.";
        }

        long expanded = 0;
        byte[] buffer = new byte[81_920];
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            string name = entry.FullName;
            if (name.StartsWith('/') || name.Contains('\\') || name.Contains(':') || name.Split('/').Contains(".."))
            {
                return $"Entry '{UploadFileName.Printable(name)}' has an unsafe path.";
            }

            if (NestedArchives.Contains(Path.GetExtension(name)))
            {
                return $"Entry '{UploadFileName.Printable(name)}' is a nested archive.";
            }

            if (entry.CompressedLength > 0 && entry.Length / entry.CompressedLength > o.MaxCompressionRatio)
            {
                return $"Entry '{UploadFileName.Printable(name)}' has compression ratio {entry.Length / entry.CompressedLength:N0}:1.";
            }

            // The sizes in the header can lie, so count the bytes that really come out
            await using Stream content = await entry.OpenAsync(ct);
            int read;
            while ((read = await content.ReadAsync(buffer, ct)) > 0)
            {
                expanded += read;
                if (expanded > o.MaxExpandedBytes)
                {
                    return $"Expanded size exceeds {o.MaxExpandedBytes:N0} bytes.";
                }
            }
        }

        return null;
    }
}
