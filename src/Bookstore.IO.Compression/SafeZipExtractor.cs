using System.IO.Compression;

namespace Bookstore.IO.Compression;

public sealed record ExtractionLimits(int MaxEntries, long MaxTotalBytes, double MaxCompressionRatio);

// Extracts an uploaded zip without trusting entry names or sizes.
public static class SafeZipExtractor
{
    public static async Task<int> ExtractAsync(Stream zip, string destination, ExtractionLimits limits, CancellationToken ct = default)
    {
        string root = Path.GetFullPath(destination);
        if (!Path.EndsInDirectorySeparator(root)) root += Path.DirectorySeparatorChar;
        StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        await using ZipArchive archive = await ZipArchive.CreateAsync(zip, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null, ct);
        if (archive.Entries.Count > limits.MaxEntries)
            throw new InvalidDataException($"Archive has {archive.Entries.Count} entries, limit is {limits.MaxEntries}.");

        byte[] buffer = new byte[81920];
        long total = 0;
        int files = 0;
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            // Path.Join, Path.Combine'ın aksine kök dizinle başlayan bir entry adında hedefi değiştirmez.
            string target = Path.GetFullPath(Path.Join(root, entry.FullName));
            if (!target.StartsWith(root, comparison))
                throw new InvalidDataException($"Entry '{entry.FullName}' is outside the target directory.");

            if (entry.FullName.EndsWith('/'))
            {
                Directory.CreateDirectory(target);
                continue;
            }
            if (entry.CompressedLength > 0 && (double)entry.Length / entry.CompressedLength > limits.MaxCompressionRatio)
                throw new InvalidDataException($"Entry '{entry.FullName}' has compression ratio {entry.Length / entry.CompressedLength:N0}:1.");

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            await using Stream input = await entry.OpenAsync(ct);
            await using var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            int read;
            while ((read = await input.ReadAsync(buffer, ct)) > 0)
            {
                // Header'daki Length'e güvenme: gerçekten açılan byte'ları say.
                total += read;
                if (total > limits.MaxTotalBytes)
                    throw new InvalidDataException($"Extracted size exceeds {limits.MaxTotalBytes:N0} bytes.");
                await output.WriteAsync(buffer.AsMemory(0, read), ct);
            }
            files++;
        }
        return files;
    }
}
