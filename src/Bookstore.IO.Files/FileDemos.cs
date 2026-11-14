using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Bookstore.Ordering;
using Microsoft.Win32.SafeHandles;

namespace Bookstore.IO.Files;

public static class FileDemos
{
    private const string OutDir = "out";

    private static string EnsureOrdersFile(int count = 200_000)
    {
        Directory.CreateDirectory(OutDir);
        string path = Path.Join(OutDir, $"orders-{count}.jsonl");
        if (!File.Exists(path))
        {
            using var writer = new StreamWriter(path);
            foreach (var order in SampleOrders.Generate(count))
            {
                writer.WriteLine(JsonSerializer.Serialize(order));
            }
        }
        return path;
    }

    public static async Task OptionsAsync()
    {
        Directory.CreateDirectory(OutDir);
        string path = Path.Join(OutDir, "invoice-BS-0000042.pdf");
        File.Delete(path);

        var options = new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            Share = FileShare.None,
            Options = FileOptions.Asynchronous | FileOptions.WriteThrough,
            BufferSize = 0,
            PreallocationSize = 256 * 1024
        };

        await using var stream = new FileStream(path, options);
        Console.WriteLine($"IsAsync: {stream.IsAsync}, Length after open: {stream.Length}");
        await stream.WriteAsync(new byte[10_000]);
        Console.WriteLine($"Length after writing 10,000 bytes: {stream.Length}, Position: {stream.Position}");
    }

    public static async Task BufferingAsync()
    {
        Directory.CreateDirectory(OutDir);
        byte[] line = Encoding.UTF8.GetBytes("BS-0000001;9780441172719;2;349.90\n");
        foreach (int bufferSize in new[] { 0, 4096, 65_536 })
        {
            string path = Path.Join(OutDir, $"lines-{bufferSize}.csv");
            var sw = Stopwatch.StartNew();
            await using (var fs = new FileStream(path, new FileStreamOptions
            {
                Mode = FileMode.Create,
                Access = FileAccess.Write,
                Options = FileOptions.Asynchronous,
                BufferSize = bufferSize
            }))
            {
                for (int i = 0; i < 100_000; i++)
                {
                    await fs.WriteAsync(line);
                }
            }
            Console.WriteLine($"BufferSize {bufferSize,6}: {sw.ElapsedMilliseconds,5} ms");
        }
    }

    public static async Task ReadLinesAsync()
    {
        string path = EnsureOrdersFile();
        Console.WriteLine($"File: {new FileInfo(path).Length:N0} bytes");

        long before = GC.GetTotalAllocatedBytes(precise: true);
        string all = await File.ReadAllTextAsync(path);
        long allocated = GC.GetTotalAllocatedBytes(precise: true) - before;
        Console.WriteLine($"ReadAllTextAsync: one string of {all.Length:N0} chars ({all.Length * sizeof(char):N0} bytes), " +
                          $"allocated {allocated:N0} bytes");

        before = GC.GetTotalAllocatedBytes(precise: true);
        int count = 0;
        int longest = 0;
        await foreach (string line in File.ReadLinesAsync(path))
        {
            count++;
            longest = Math.Max(longest, line.Length);
        }
        allocated = GC.GetTotalAllocatedBytes(precise: true) - before;
        Console.WriteLine($"ReadLinesAsync:   {count:N0} lines, longest {longest} chars, allocated {allocated:N0} bytes");
    }

    public static async Task RandomAccessAsync()
    {
        string path = EnsureOrdersFile();
        using SafeFileHandle handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            FileOptions.Asynchronous);
        long length = RandomAccess.GetLength(handle);
        const int parts = 4;
        long chunk = (length + parts - 1) / parts;

        // Each task reads its own range of the same handle; there is no shared Position.
        long[] counts = await Task.WhenAll(Enumerable.Range(0, parts).Select(async p =>
        {
            long start = p * chunk;
            long end = Math.Min(start + chunk, length);
            byte[] buffer = new byte[1 << 20];
            long lines = 0;
            for (long offset = start; offset < end;)
            {
                int toRead = (int)Math.Min(buffer.Length, end - offset);
                int n = await RandomAccess.ReadAsync(handle, buffer.AsMemory(0, toRead), offset);
                if (n == 0) break;
                lines += buffer.AsSpan(0, n).Count((byte)'\n');
                offset += n;
            }
            return lines;
        }));

        Console.WriteLine($"Length {length:N0}, {parts} ranges of {chunk:N0} bytes");
        Console.WriteLine($"Lines per range: {string.Join(" + ", counts.Select(c => c.ToString("N0")))} = {counts.Sum():N0}");
    }

    public static async Task AtomicAsync()
    {
        Directory.CreateDirectory(OutDir);
        string path = Path.Join(OutDir, "invoice-BS-0000042.pdf");
        byte[] v1 = Encoding.ASCII.GetBytes(new string('A', 200_000));
        byte[] v2 = Encoding.ASCII.GetBytes(new string('B', 200_000));

        await File.WriteAllBytesAsync(path, v1);
        try
        {
            await using var fs = new FileStream(path, FileMode.Create);
            await fs.WriteAsync(v2.AsMemory(0, 80_000));
            throw new IOException("renderer crashed");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Direct write:  {ex.Message}, file is now {new FileInfo(path).Length:N0} bytes");
        }

        await File.WriteAllBytesAsync(path, v1);
        try
        {
            await AtomicFile.WriteAllBytesAsync(path, v2, new CancellationTokenSource(TimeSpan.Zero).Token);
        }
        catch (OperationCanceledException)
        {
            byte first = File.ReadAllBytes(path)[0];
            Console.WriteLine($"Atomic write:  cancelled, file is {new FileInfo(path).Length:N0} bytes of '{(char)first}'");
        }

        await AtomicFile.WriteAllBytesAsync(path, v2);
        Console.WriteLine($"Atomic write:  done, file is {new FileInfo(path).Length:N0} bytes of '{(char)File.ReadAllBytes(path)[0]}'");
        Console.WriteLine($"Temp files left: {Directory.GetFiles(OutDir, ".*.tmp").Length}");
    }

    public static async Task CreateNewAsync()
    {
        Directory.CreateDirectory(OutDir);
        string path = Path.Join(OutDir, "invoice-BS-0000077.pdf");
        File.Delete(path);

        var results = await Task.WhenAll(Enumerable.Range(1, 4).Select(worker => Task.Run(async () =>
        {
            try
            {
                await using var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await fs.WriteAsync(Encoding.ASCII.GetBytes($"worker {worker}"));
                return $"worker {worker}: created";
            }
            catch (IOException ex) when (File.Exists(path))
            {
                return $"worker {worker}: {ex.GetType().Name}, file already exists";
            }
        })));
        foreach (var r in results) Console.WriteLine(r);
    }

    public static void Paths()
    {
        Console.WriteLine($"Combine: {Path.Combine("invoices", "/etc/passwd")}");
        Console.WriteLine($"Join:    {Path.Join("invoices", "/etc/passwd")}");
        string root = Path.GetFullPath("invoices");
        string requested = Path.GetFullPath(Path.Join(root, "../appsettings.json"));
        bool inside = requested.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal);
        Console.WriteLine($"'../appsettings.json' stays inside invoices: {inside}");
    }
}
