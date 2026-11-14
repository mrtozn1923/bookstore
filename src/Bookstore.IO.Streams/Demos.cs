using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Bookstore.Ordering;

namespace Bookstore.IO.Streams;

public static class Demos
{
    private const string OutDir = "out";

    public static async Task CapabilitiesAsync()
    {
        Directory.CreateDirectory(OutDir);
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
        using var server = await listener.AcceptTcpClientAsync();

        var file = new FileStream(Path.Join(OutDir, "caps.bin"), FileMode.Create);
        Stream[] streams =
        [
            new MemoryStream(),
            file,
            new BufferedStream(file),
            new GZipStream(new MemoryStream(), CompressionLevel.Fastest),
            client.GetStream()
        ];

        Console.WriteLine($"{"Stream",-16} {"CanRead",-8} {"CanWrite",-9} {"CanSeek",-8} Length");
        foreach (var s in streams)
        {
            string length;
            try { length = s.Length.ToString(); }
            catch (NotSupportedException) { length = "NotSupportedException"; }
            Console.WriteLine($"{s.GetType().Name,-16} {s.CanRead,-8} {s.CanWrite,-9} {s.CanSeek,-8} {length}");
        }

        foreach (var s in streams.Reverse()) s.Dispose();
    }

    // Export header: "BKEX" + version (2) + record count (8) + flags (2) = 16 bytes.
    private static byte[] CreateHeader(long recordCount)
    {
        var header = new byte[16];
        "BKEX"u8.CopyTo(header);
        BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(4), 1);
        BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(6), recordCount);
        BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(14), 0);
        return header;
    }

    public static async Task ShortReadAsync()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;

        // Sender: the 16-byte header goes out in two pieces.
        var sender = Task.Run(async () =>
        {
            using var c = await listener.AcceptTcpClientAsync();
            var ns = c.GetStream();
            byte[] header = CreateHeader(120_000);
            for (int round = 0; round < 2; round++)
            {
                await ns.WriteAsync(header.AsMemory(0, 7));
                await Task.Delay(50);
                await ns.WriteAsync(header.AsMemory(7));
            }
            await ns.WriteAsync(header.AsMemory(0, 10)); // truncated third header
        });

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        var stream = client.GetStream();
        var buffer = new byte[16];

        int n = stream.Read(buffer, 0, buffer.Length);
        Console.WriteLine($"Read returned {n} of {buffer.Length} bytes");
        stream.ReadExactly(buffer, n, buffer.Length - n); // complete the first header

        stream.ReadExactly(buffer);
        long count = BinaryPrimitives.ReadInt64LittleEndian(buffer.AsSpan(6));
        Console.WriteLine($"ReadExactly: magic={Encoding.ASCII.GetString(buffer, 0, 4)}, records={count}");

        await sender; // the sender has closed the connection

        int got = stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
        Console.WriteLine($"ReadAtLeast(throwOnEndOfStream: false) returned {got}");

        try
        {
            stream.ReadExactly(buffer);
        }
        catch (EndOfStreamException ex)
        {
            Console.WriteLine($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    public static async Task ExportAsync()
    {
        Directory.CreateDirectory(OutDir);
        string path = Path.Join(OutDir, "orders-2026-11.jsonl.gz");
        var orders = SampleOrders.Generate(100_000).ToList();

        var sw = Stopwatch.StartNew();
        await OrderExport.WriteAsync(path, orders);
        Console.WriteLine($"Written: {new FileInfo(path).Length:N0} bytes in {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        int read = 0;
        decimal total = 0;
        await foreach (var order in OrderExport.ReadAsync(path))
        {
            read++;
            total += order.Total;
        }
        Console.WriteLine($"Read back: {read:N0} orders, total {total:N2} TRY in {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"Expected:  {orders.Count:N0} orders, total {orders.Sum(o => o.Total):N2} TRY");

        // Compress then encrypt vs encrypt then compress.
        using var aes = Aes.Create();
        long good = await WriteEncryptedAsync(orders, aes, compressFirst: true);
        long bad = await WriteEncryptedAsync(orders, aes, compressFirst: false);
        Console.WriteLine($"gzip -> AES: {good:N0} bytes");
        Console.WriteLine($"AES -> gzip: {bad:N0} bytes");
    }

    private static async Task<long> WriteEncryptedAsync(List<Order> orders, Aes aes, bool compressFirst)
    {
        var target = new CountingStream(Stream.Null);
        Stream top;
        if (compressFirst)
        {
            // writer -> gzip -> crypto -> target
            var crypto = new CryptoStream(target, aes.CreateEncryptor(), CryptoStreamMode.Write);
            top = new GZipStream(crypto, CompressionLevel.Optimal);
        }
        else
        {
            // writer -> crypto -> gzip -> target
            var gzip = new GZipStream(target, CompressionLevel.Optimal, leaveOpen: true);
            top = new CryptoStream(gzip, aes.CreateEncryptor(), CryptoStreamMode.Write);
        }

        await using (var writer = new StreamWriter(top))
        {
            foreach (var order in orders)
            {
                await writer.WriteLineAsync(JsonSerializer.Serialize(order));
            }
        }
        return target.BytesWritten;
    }

    public static void Truncated()
    {
        byte[] payload = Encoding.UTF8.GetBytes(string.Join('\n',
            SampleOrders.Generate(2_000).Select(o => JsonSerializer.Serialize(o))));

        var ms = new MemoryStream();
        var gzip = new GZipStream(ms, CompressionLevel.Optimal);
        gzip.Write(payload);
        byte[] notFlushed = ms.ToArray();
        Console.WriteLine($"Without Flush/Dispose: {notFlushed.Length:N0} bytes in MemoryStream");
        gzip.Flush();
        byte[] flushedOnly = ms.ToArray();
        Console.WriteLine($"After Flush:           {flushedOnly.Length:N0} bytes");
        gzip.Dispose();
        Console.WriteLine($"After Dispose:         {ms.ToArray().Length:N0} bytes");

        TryDecompress("not flushed", notFlushed, payload.Length);
        TryDecompress("flushed, not disposed", flushedOnly, payload.Length);
        TryDecompress("disposed", ms.ToArray(), payload.Length);

        Directory.CreateDirectory(OutDir);
        File.WriteAllBytes(Path.Join(OutDir, "not-flushed.gz"), notFlushed);
        File.WriteAllBytes(Path.Join(OutDir, "flushed.gz"), flushedOnly);
        File.WriteAllBytes(Path.Join(OutDir, "disposed.gz"), ms.ToArray());
    }

    private static void TryDecompress(string label, byte[] data, int expected)
    {
        try
        {
            using var gz = new GZipStream(new MemoryStream(data), CompressionMode.Decompress);
            var output = new MemoryStream();
            gz.CopyTo(output);
            Console.WriteLine($"{label}: {output.Length:N0} of {expected:N0} bytes, no exception");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{label}: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public static void LeaveOpen()
    {
        byte[] data = Encoding.UTF8.GetBytes("BS-0000001,Dune,2,349.90\n");

        var ms = new MemoryStream();
        using (var gzip = new GZipStream(ms, CompressionLevel.Fastest))
        {
            gzip.Write(data);
        }
        try
        {
            ms.Position = 0;
        }
        catch (ObjectDisposedException ex)
        {
            Console.WriteLine($"leaveOpen: false -> {ex.GetType().Name}: {ex.Message}");
        }

        var ms2 = new MemoryStream();
        using (var gzip = new GZipStream(ms2, CompressionLevel.Fastest, leaveOpen: true))
        {
            gzip.Write(data);
        }
        ms2.Position = 0;
        Console.WriteLine($"leaveOpen: true  -> {ms2.Length} bytes, Position reset to {ms2.Position}");
    }

    public static async Task CountingAsync()
    {
        Directory.CreateDirectory(OutDir);
        string path = Path.Join(OutDir, "orders-counted.jsonl.gz");
        var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        var compressed = new CountingStream(file);
        var gzip = new GZipStream(compressed, CompressionLevel.Optimal);
        var raw = new CountingStream(gzip);

        await using (var writer = new StreamWriter(raw))
        {
            foreach (var order in SampleOrders.Generate(50_000))
            {
                await writer.WriteLineAsync(JsonSerializer.Serialize(order));
            }
        }
        Console.WriteLine($"JSON bytes into gzip:   {raw.BytesWritten:N0}");
        Console.WriteLine($"gzip bytes into file:   {compressed.BytesWritten:N0}");
        Console.WriteLine($"File length on disk:    {new FileInfo(path).Length:N0}");

        byte[] rented = ArrayPool<byte>.Shared.Rent(81_920);
        Console.WriteLine($"ArrayPool.Rent(81920) returned {rented.Length:N0} bytes");
        ArrayPool<byte>.Shared.Return(rented);

        await using var source = File.OpenRead(path);
        await using var target = new CountingStream(Stream.Null);
        long copied = await CopyWithPooledBufferAsync(source, target);
        Console.WriteLine($"Copied {copied:N0} bytes with a pooled buffer");
    }

    public static async Task<long> CopyWithPooledBufferAsync(Stream source, Stream destination,
        int bufferSize = 81_920, CancellationToken ct = default)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        long total = 0;
        try
        {
            int n;
            while ((n = await source.ReadAsync(buffer.AsMemory(0, bufferSize), ct)) > 0)
            {
                await destination.WriteAsync(buffer.AsMemory(0, n), ct);
                total += n;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
        return total;
    }

    public static async Task MemoryAsync()
    {
        Directory.CreateDirectory(OutDir);
        var orders = SampleOrders.Generate(200_000).ToList();

        long before = GC.GetTotalAllocatedBytes(precise: true);
        var ms = new MemoryStream();
        await using (var writer = new StreamWriter(ms, leaveOpen: true))
        {
            foreach (var o in orders) await writer.WriteLineAsync(JsonSerializer.Serialize(o));
        }
        long msAlloc = GC.GetTotalAllocatedBytes(precise: true) - before;
        Console.WriteLine($"MemoryStream: Length {ms.Length:N0}, Capacity {ms.Capacity:N0}, allocated {msAlloc:N0} bytes");

        before = GC.GetTotalAllocatedBytes(precise: true);
        await using (var file = new FileStream(Path.Join(OutDir, "orders.jsonl"), FileMode.Create,
            FileAccess.Write, FileShare.None, 4096, useAsync: true))
        await using (var writer = new StreamWriter(file))
        {
            foreach (var o in orders) await writer.WriteLineAsync(JsonSerializer.Serialize(o));
        }
        long fileAlloc = GC.GetTotalAllocatedBytes(precise: true) - before;
        Console.WriteLine($"FileStream:   allocated {fileAlloc:N0} bytes");
    }
}
