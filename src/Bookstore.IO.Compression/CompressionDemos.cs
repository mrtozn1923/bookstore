using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using Bookstore.IO.Xml.Invoices;
using Bookstore.Ordering;

namespace Bookstore.IO.Compression;

public static class CompressionDemos
{
    private static readonly string Out = "out";

    public static byte[] CreateNdjson(int count)
    {
        using var buffer = new MemoryStream();
        foreach (Order order in SampleOrders.Generate(count))
        {
            JsonSerializer.Serialize(buffer, order);
            buffer.WriteByte((byte)'\n');
        }
        return buffer.ToArray();
    }

    public static void Formats()
    {
        byte[] data = CreateNdjson(20_000);
        Console.WriteLine($"Input: {data.Length:N0} bytes NDJSON");
        Console.WriteLine($"{"Format",-8} {"Level",-13} {"Bytes",11} {"Ratio",6} {"ms",6}  Header");
        var formats = new (string Name, Func<Stream, CompressionLevel, Stream> Create)[]
        {
            ("Deflate", (s, l) => new DeflateStream(s, l, leaveOpen: true)),
            ("ZLib", (s, l) => new ZLibStream(s, l, leaveOpen: true)),
            ("GZip", (s, l) => new GZipStream(s, l, leaveOpen: true)),
            ("Brotli", (s, l) => new BrotliStream(s, l, leaveOpen: true))
        };
        foreach (var (name, create) in formats)
        {
            foreach (CompressionLevel level in new[] { CompressionLevel.Fastest, CompressionLevel.Optimal, CompressionLevel.SmallestSize })
            {
                var (bytes, ms) = Measure(data, s => create(s, level));
                Console.WriteLine($"{name,-8} {level,-13} {bytes.Length,11:N0} {(double)data.Length / bytes.Length,5:0.0}x {ms,6:0.0}  {Convert.ToHexString(bytes, 0, 3)}");
            }
        }
    }

    public static void Options()
    {
        byte[] data = CreateNdjson(20_000);
        foreach (int level in new[] { 1, 6, 9 })
        {
            var options = new ZLibCompressionOptions { CompressionLevel = level, CompressionStrategy = ZLibCompressionStrategy.Default };
            var (bytes, ms) = Measure(data, s => new ZLibStream(s, options, leaveOpen: true));
            Console.WriteLine($"ZLib   level {level,2}:   {bytes.Length,9:N0} bytes {ms,6:0.0} ms");
        }
        foreach (int quality in new[] { 1, 4, 9, 11 })
        {
            var options = new BrotliCompressionOptions { Quality = quality };
            var (bytes, ms) = Measure(data, s => new BrotliStream(s, options, leaveOpen: true));
            Console.WriteLine($"Brotli quality {quality,2}: {bytes.Length,9:N0} bytes {ms,6:0.0} ms");
        }
    }

    // En iyi 5 ölçümden en kısası; tek ölçüm JIT ve ısınmayla oynar.
    private static (byte[] Bytes, double Ms) Measure(byte[] data, Func<Stream, Stream> create)
    {
        byte[] result = [];
        double best = double.MaxValue;
        for (int i = 0; i < 5; i++)
        {
            var output = new MemoryStream();
            var sw = Stopwatch.StartNew();
            using (Stream compressor = create(output)) compressor.Write(data);
            sw.Stop();
            best = Math.Min(best, sw.Elapsed.TotalMilliseconds);
            result = output.ToArray();
        }
        return (result, best);
    }

    public static async Task ZipAsync(int count)
    {
        Directory.CreateDirectory(Out);
        string path = Path.Join(Out, "invoices.zip");
        var sw = Stopwatch.StartNew();
        await using (var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
        {
            await InvoiceZip.WriteAsync(file, SampleOrders.Generate(count));
        }
        Console.WriteLine($"Wrote {count:N0} invoices to {path}: {new FileInfo(path).Length:N0} bytes in {sw.ElapsedMilliseconds:N0} ms");

        await using ZipArchive archive = await ZipFile.OpenReadAsync(path);
        foreach (ZipArchiveEntry entry in archive.Entries.Take(2))
        {
            Console.WriteLine($"  {entry.FullName}: Length {entry.Length:N0}, CompressedLength {entry.CompressedLength:N0}");
        }
        Console.WriteLine($"  Sum of Length: {archive.Entries.Sum(e => e.Length):N0}, sum of CompressedLength: {archive.Entries.Sum(e => e.CompressedLength):N0}");
    }

    public static async Task AsyncCheckAsync(int count)
    {
        var counter = new WriteCountingStream(Stream.Null);
        await InvoiceZip.WriteAsync(counter, SampleOrders.Generate(count));
        Console.WriteLine($"Runtime {Environment.Version}: {count:N0} entries");
        Console.WriteLine($"  async writes: {counter.AsyncWrites,6:N0} ({counter.AsyncBytes:N0} bytes)");
        Console.WriteLine($"  sync writes:  {counter.SyncWrites,6:N0} ({counter.SyncBytes:N0} bytes)");
    }

    public static async Task TarAsync(int count)
    {
        string dir = Path.Join(Out, "invoices");
        if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        Directory.CreateDirectory(dir);
        foreach (Order order in SampleOrders.Generate(count))
        {
            InvoiceXml.Create(order, InvoiceZip.IssueDate).Save(Path.Join(dir, $"{order.OrderNumber}.xml"));
        }

        string tarGz = Path.Join(Out, "invoices.tar.gz");
        await using (var file = File.Create(tarGz))
        await using (var gzip = new GZipStream(file, CompressionLevel.Optimal))
        {
            await TarFile.CreateFromDirectoryAsync(dir, gzip, includeBaseDirectory: false);
        }

        string zip = Path.Join(Out, "invoices-from-dir.zip");
        await using (var file = File.Create(zip))
        {
            // .NET 8: ZipFile'ın stream alan overload'ı.
            ZipFile.CreateFromDirectory(dir, file, CompressionLevel.Optimal, includeBaseDirectory: false);
        }

        long raw = Directory.EnumerateFiles(dir).Sum(f => new FileInfo(f).Length);
        Console.WriteLine($"{count:N0} files, {raw:N0} bytes");
        Console.WriteLine($"zip:    {new FileInfo(zip).Length,10:N0} bytes");
        Console.WriteLine($"tar.gz: {new FileInfo(tarGz).Length,10:N0} bytes");

        await using var reader = new TarReader(new GZipStream(File.OpenRead(tarGz), CompressionMode.Decompress));
        TarEntry first = (await reader.GetNextEntryAsync())!;
        Console.WriteLine($"First tar entry: {first.Name}, {first.Length:N0} bytes, format {first.Format}");
    }

    public static async Task TwoEntriesAsync()
    {
        await using ZipArchive archive = await ZipArchive.CreateAsync(new MemoryStream(), ZipArchiveMode.Create, leaveOpen: false, entryNameEncoding: null);
        await using Stream first = await archive.CreateEntry("a.xml").OpenAsync();
        try
        {
            await using Stream second = await archive.CreateEntry("b.xml").OpenAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    public static async Task SlipAsync()
    {
        string zipPath = Path.Join(Out, "slip.zip");
        Directory.CreateDirectory(Out);
        using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            Write(archive, "invoices/BS-0000001.xml", "<Invoice />");
            Write(archive, "../outside.txt", "written outside the target");
        }

        string target = Path.Join(Out, "extract", "builtin");
        try
        {
            ZipFile.ExtractToDirectory(zipPath, target);
        }
        catch (IOException ex)
        {
            Console.WriteLine($"ZipFile.ExtractToDirectory: {ex.GetType().Name}: {ex.Message}");
        }

        // Yanlış: entry adını doğrudan path'e eklemek.
        string naiveTarget = Path.Join(Out, "extract", "naive");
        Directory.CreateDirectory(naiveTarget);
        using (ZipArchive archive = ZipFile.OpenRead(zipPath))
        {
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string path = Path.Combine(naiveTarget, entry.FullName);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                entry.ExtractToFile(path, overwrite: true);
                Console.WriteLine($"Naive: {entry.FullName} -> {Path.GetRelativePath(Out, Path.GetFullPath(path))}");
            }
        }

        try
        {
            await SafeZipExtractor.ExtractAsync(File.OpenRead(zipPath), Path.Join(Out, "extract", "safe"), new ExtractionLimits(1_000, 100_000_000, 100));
        }
        catch (InvalidDataException ex)
        {
            Console.WriteLine($"SafeZipExtractor: {ex.Message}");
        }

        static void Write(ZipArchive archive, string name, string content)
        {
            using var writer = new StreamWriter(archive.CreateEntry(name).Open());
            writer.Write(content);
        }
    }

    public static async Task BombAsync()
    {
        string zipPath = Path.Join(Out, "large.zip");
        Directory.CreateDirectory(Out);
        using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        using (Stream entry = archive.CreateEntry("report.csv", CompressionLevel.SmallestSize).Open())
        {
            byte[] zeros = new byte[1024 * 1024];
            for (int i = 0; i < 1024; i++) entry.Write(zeros);
        }
        using (ZipArchive archive = ZipFile.OpenRead(zipPath))
        {
            ZipArchiveEntry e = archive.Entries[0];
            Console.WriteLine($"{new FileInfo(zipPath).Length:N0} byte zip, entry Length {e.Length:N0}, CompressedLength {e.CompressedLength:N0}");
        }

        var limits = new ExtractionLimits(MaxEntries: 1_000, MaxTotalBytes: 100_000_000, MaxCompressionRatio: 100);
        await TryExtract("ratio check", limits);
        await TryExtract("size check", limits with { MaxCompressionRatio = double.MaxValue });

        async Task TryExtract(string label, ExtractionLimits l)
        {
            string target = Path.Join(Out, "extract", label.Replace(' ', '-'));
            if (Directory.Exists(target)) Directory.Delete(target, recursive: true);
            try
            {
                await SafeZipExtractor.ExtractAsync(File.OpenRead(zipPath), target, l);
            }
            catch (InvalidDataException ex)
            {
                Console.WriteLine($"{label}: {ex.Message}");
            }
        }
    }
}
