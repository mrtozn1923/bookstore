using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Formats.Tar;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Bookstore.Ordering;
using Microsoft.Win32.SafeHandles;

namespace Bookstore.IO.Net11;

public static class Net11Demos
{
    private static void Try(string label, Action action)
    {
        try { action(); }
        catch (Exception ex) { Console.WriteLine($"{label}: {ex.GetType().Name}: {ex.Message}"); }
    }

    public static async Task JsonAsync()
    {
        OrderReference byId = 42, byNumber = "BS-0000042";
        Console.WriteLine($"Union: {JsonSerializer.Serialize(byId)} and {JsonSerializer.Serialize(byNumber)}");
        OrderReference read = JsonSerializer.Deserialize<OrderReference>("\"BS-0000007\"");
        string text = read switch
        {
            int id => $"id {id}",
            string number => $"number {number}"
        };
        Console.WriteLine($"Union read: {text}");

        const string declined = """{"Reason":"insufficient_funds","RetryAfterSeconds":3600}""";
        Try("Without classifier", () => JsonSerializer.Deserialize<PaymentResultByKind>(declined));
        PaymentResult result = JsonSerializer.Deserialize<PaymentResult>(declined);
        Console.WriteLine($"Structural classifier: {result.Value}");
        Console.WriteLine($"Union write: {JsonSerializer.Serialize((PaymentResult)new PaymentApproved("A-1001", 349.90m))}");

        ShippingOption[] options = [new StandardShipping(3), new ExpressShipping(new DateOnly(2026, 11, 16))];
        Console.WriteLine($"Closed hierarchy: {JsonSerializer.Serialize(options)}");

        Console.WriteLine($"Type-level attributes: {JsonSerializer.Serialize(new StockAlert { Isbn = "9780441172719", QuantityOnHand = 2 })}");

        Directory.CreateDirectory("out");
        string path = Path.Join("out", "orders.jsonl");
        await using (var file = File.Create(path))
        {
            await JsonSerializer.SerializeAsyncEnumerable(file, Orders(1_000), topLevelValues: true);
        }
        string[] lines = File.ReadLines(path).Take(2).ToArray();
        Console.WriteLine($"JSON Lines: {new FileInfo(path).Length:N0} bytes, last byte 0x{File.ReadAllBytes(path)[^1]:X2}");
        Console.WriteLine($"  {lines[0][..60]}...");
        int count = 0;
        await using (var file = File.OpenRead(path))
        {
            await foreach (Order? order in JsonSerializer.DeserializeAsyncEnumerable<Order>(file, topLevelValues: true)) count++;
        }
        Console.WriteLine($"  read back {count:N0} orders");

        using var buffer = new MemoryStream();
        using var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true });
        writer.WriteStartObject(); writer.WriteString("isbn", "9780441172719"); writer.WriteEndObject(); writer.Flush();
        using var second = new MemoryStream();
        // Aynı writer başka bir hedef ve başka ayarlarla yeniden kullanılır.
        writer.Reset(second, new JsonWriterOptions { Indented = false });
        writer.WriteStartObject(); writer.WriteString("isbn", "9780141439587"); writer.WriteEndObject(); writer.Flush();
        Console.WriteLine($"Utf8JsonWriter.Reset: {buffer.Length} bytes indented, then {Encoding.UTF8.GetString(second.ToArray())}");
    }

    private static async IAsyncEnumerable<Order> Orders(int count)
    {
        foreach (Order order in SampleOrders.Generate(count))
        {
            yield return order;
            await Task.Yield();
        }
    }

    public static async Task StreamsAsync()
    {
        string json = JsonSerializer.Serialize(SampleOrders.Generate(1).Single());

        // string'den byte[] üretmeden Stream.
        using (var stream = new StringStream(json, Encoding.UTF8))
        {
            Order order = (await JsonSerializer.DeserializeAsync<Order>(stream))!;
            Console.WriteLine($"StringStream: {order.OrderNumber}, CanSeek {stream.CanSeek}");
        }

        ReadOnlyMemory<byte> payload = Encoding.UTF8.GetBytes(json);
        using (var stream = new ReadOnlyMemoryStream(payload))
        {
            Order order = (await JsonSerializer.DeserializeAsync<Order>(stream))!;
            Console.WriteLine($"ReadOnlyMemoryStream: {order.OrderNumber}, Length {stream.Length}, CanWrite {stream.CanWrite}");
        }

        // Pipe'tan gelen çok segmentli bir ReadOnlySequence, tek bir array'e kopyalanmadan stream olur.
        var pipe = new Pipe(new PipeOptions(minimumSegmentSize: 128));
        for (int i = 0; i < payload.Length; i += 100)
        {
            await pipe.Writer.WriteAsync(payload[i..Math.Min(i + 100, payload.Length)]);
        }
        await pipe.Writer.CompleteAsync();
        ReadResult result = await pipe.Reader.ReadAsync();
        while (!result.IsCompleted)
        {
            pipe.Reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
            result = await pipe.Reader.ReadAsync();
        }
        using (var stream = new ReadOnlySequenceStream(result.Buffer))
        {
            Order order = (await JsonSerializer.DeserializeAsync<Order>(stream))!;
            Console.WriteLine($"ReadOnlySequenceStream: {order.OrderNumber}, single segment {result.Buffer.IsSingleSegment}");
        }
        pipe.Reader.AdvanceTo(result.Buffer.End);
        await pipe.Reader.CompleteAsync();

        byte[] fixedBuffer = new byte[64];
        using (var stream = new WritableMemoryStream(fixedBuffer))
        {
            Try("WritableMemoryStream (64 bytes)", () => JsonSerializer.Serialize(stream, SampleOrders.Generate(1).Single()));
        }

        using SafeFileHandle file = File.OpenHandle(Path.Join("out", "orders.jsonl"));
        SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle readEnd, out SafeFileHandle writeEnd, asyncRead: false, asyncWrite: false);
        using (readEnd)
        using (writeEnd)
        {
            Console.WriteLine($"SafeFileHandle.Type: file {file.Type}, pipe {readEnd.Type}");
            // .NET 11: RandomAccess seek edilemeyen handle'larla da çalışır; offset yok sayılır.
            RandomAccess.Write(writeEnd, "BS-0000042\n"u8, fileOffset: 0);
            byte[] received = new byte[11];
            int read = RandomAccess.Read(readEnd, received, fileOffset: 0);
            Console.WriteLine($"RandomAccess over a pipe: {read} bytes, {Encoding.ASCII.GetString(received, 0, read).TrimEnd()}");
        }
    }

    public static void Compression()
    {
        byte[] data;
        using (var buffer = new MemoryStream())
        {
            foreach (Order order in SampleOrders.Generate(20_000))
            {
                JsonSerializer.Serialize(buffer, order);
                buffer.WriteByte((byte)'\n');
            }
            data = buffer.ToArray();
        }
        Console.WriteLine($"Input: {data.Length:N0} bytes NDJSON");
        Measure("GZip Optimal", s => new GZipStream(s, CompressionLevel.Optimal, leaveOpen: true));
        Measure("Brotli quality 9", s => new BrotliStream(s, new BrotliCompressionOptions { Quality = 9 }, leaveOpen: true));
        Measure("Zstandard Fastest", s => new ZstandardStream(s, CompressionLevel.Fastest, leaveOpen: true));
        Measure("Zstandard Optimal", s => new ZstandardStream(s, CompressionLevel.Optimal, leaveOpen: true));
        Measure("Zstandard quality 19", s => new ZstandardStream(s, new ZstandardCompressionOptions { Quality = 19 }, leaveOpen: true));

        byte[] destination = new byte[data.Length];
        bool ok = ZLibEncoder.TryCompress(data, destination, out int written);
        Console.WriteLine($"ZLibEncoder.TryCompress: {ok}, {written:N0} bytes, no Stream");

        void Measure(string label, Func<Stream, Stream> create)
        {
            long size = 0;
            double best = double.MaxValue;
            for (int i = 0; i < 5; i++)
            {
                var output = new MemoryStream();
                var sw = Stopwatch.StartNew();
                using (Stream compressor = create(output)) compressor.Write(data);
                best = Math.Min(best, sw.Elapsed.TotalMilliseconds);
                size = output.Length;
            }
            Console.WriteLine($"{label,-21} {size,9:N0} bytes {best,8:N1} ms");
        }
    }

    public static void Empty()
    {
        var output = new MemoryStream();
        using (new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true)) { }
        Console.WriteLine($"Runtime {Environment.Version}: empty GZip payload -> {output.Length} bytes");
    }

    public static void Zip()
    {
        // Sıkıştırmasız (Stored) bir entry'nin verisinde tek bir byte'ı bozuyoruz.
        var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        using (var writer = new StreamWriter(archive.CreateEntry("invoice.xml", CompressionLevel.NoCompression).Open()))
        {
            writer.Write("<Invoice><ID>BSI20260000042</ID></Invoice>");
        }
        byte[] bytes = buffer.ToArray();
        int index = bytes.AsSpan().IndexOf("BSI2026"u8);
        bytes[index] = (byte)'X';
        using (var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read))
        {
            ZipArchiveEntry entry = archive.Entries[0];
            Console.WriteLine($"Runtime {Environment.Version}: method {entry.CompressionMethod}");
            Try("Corrupted entry", () => Console.WriteLine($"Corrupted entry read: {new StreamReader(entry.Open()).ReadToEnd()}"));
        }

        var encrypted = new MemoryStream();
        using (var archive = new ZipArchive(encrypted, ZipArchiveMode.Create, leaveOpen: true))
        using (var writer = new StreamWriter(archive.CreateEntry("invoice.xml", "example-password", ZipEncryptionMethod.Aes256).Open()))
        {
            writer.Write("<Invoice />");
        }
        encrypted.Position = 0;
        using (var archive = new ZipArchive(encrypted, ZipArchiveMode.Read))
        {
            ZipArchiveEntry entry = archive.Entries[0];
            Console.WriteLine($"Encrypted: {entry.IsEncrypted}, {entry.EncryptionMethod}");
            Try("Open without password", () => entry.Open());
            using var reader = new StreamReader(entry.Open(FileAccess.Read, "example-password"));
            Console.WriteLine($"Open with password: {reader.ReadToEnd()}");
        }

        string dir = Path.Join("out", "tar-source");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Join(dir, "BS-0000042.xml"), "<Invoice />");
        var tar = new MemoryStream();
        TarFile.CreateFromDirectory(dir, tar, includeBaseDirectory: false, format: TarEntryFormat.Gnu);
        tar.Position = 0;
        using var tarReader = new TarReader(tar);
        Console.WriteLine($"Tar format: {tarReader.GetNextEntry()!.Format}");
    }
}
