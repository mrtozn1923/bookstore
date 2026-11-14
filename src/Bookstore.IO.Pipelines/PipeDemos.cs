using System.Diagnostics;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Bookstore.Ordering;

namespace Bookstore.IO.Pipelines;

public static class PipeDemos
{
    public static byte[] CreateNdjson(int count)
    {
        var ms = new MemoryStream();
        using (var writer = new StreamWriter(ms, new UTF8Encoding(false), leaveOpen: true))
        {
            foreach (var order in SampleOrders.Generate(count))
            {
                writer.Write(JsonSerializer.Serialize(order));
                writer.Write('\n');
            }
        }
        return ms.ToArray();
    }

    // Wrong on purpose: every 4 KB buffer is split on '\n' on its own.
    public static async Task NaiveAsync()
    {
        byte[] data = CreateNdjson(20_000);
        var stream = new MemoryStream(data);
        byte[] buffer = new byte[4096];
        int parsed = 0, failed = 0, n;
        while ((n = await stream.ReadAsync(buffer)) > 0)
        {
            foreach (string piece in Encoding.UTF8.GetString(buffer, 0, n).Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                try { JsonSerializer.Deserialize<Order>(piece); parsed++; }
                catch (JsonException) { failed++; }
            }
        }
        Console.WriteLine($"Naive: {parsed:N0} parsed, {failed:N0} JsonException, expected 20,000");
    }

    public static async Task SplitAsync()
    {
        byte[] data = CreateNdjson(20_000);
        var reader = PipeReader.Create(new MemoryStream(data), new StreamPipeReaderOptions(bufferSize: 4096));
        long count = await NdjsonOrders.ReadWithPipeReaderAsync(reader, _ => ValueTask.CompletedTask);
        Console.WriteLine($"PipeReader: {count:N0} orders parsed");

        var defaults = PipeOptions.Default;
        Console.WriteLine($"PipeOptions.Default: pause {defaults.PauseWriterThreshold:N0}, resume {defaults.ResumeWriterThreshold:N0}, " +
                          $"minimum segment {defaults.MinimumSegmentSize:N0}");
    }

    public static async Task ChannelAsync()
    {
        Directory.CreateDirectory("out");
        string path = Path.Join("out", "orders-100000.ndjson");
        if (!File.Exists(path)) await File.WriteAllBytesAsync(path, CreateNdjson(100_000));

        var channel = Channel.CreateBounded<Order>(new BoundedChannelOptions(1_000)
        {
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait
        });

        // Three consumers sum revenue per status.
        var totals = new decimal[Enum.GetValues<OrderStatus>().Length];
        var consumers = Enumerable.Range(0, 3).Select(_ => Task.Run(async () =>
        {
            var local = new decimal[totals.Length];
            await foreach (var order in channel.Reader.ReadAllAsync())
            {
                local[(int)order.Status] += order.Total;
            }
            lock (totals)
            {
                for (int i = 0; i < local.Length; i++) totals[i] += local[i];
            }
        })).ToArray();

        var sw = Stopwatch.StartNew();
        await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 0, FileOptions.Asynchronous);
        long count = await NdjsonOrders.ReadWithPipeReaderAsync(PipeReader.Create(file),
            order => channel.Writer.WriteAsync(order));
        channel.Writer.Complete();
        await Task.WhenAll(consumers);

        Console.WriteLine($"{count:N0} orders in {sw.ElapsedMilliseconds} ms");
        foreach (var status in Enum.GetValues<OrderStatus>())
        {
            Console.WriteLine($"{status,-10} {totals[(int)status],16:N2} TRY");
        }
    }

    public static async Task BackpressureAsync()
    {
        byte[] data = CreateNdjson(20_000);
        var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 64 * 1024, resumeWriterThreshold: 32 * 1024));
        long written = 0, consumed = 0, maxBuffered = 0;
        int pauses = 0;

        var writer = Task.Run(async () =>
        {
            // A fast producer: 8 KB at a time, as fast as the pipe allows.
            for (int offset = 0; offset < data.Length; offset += 8192)
            {
                int size = Math.Min(8192, data.Length - offset);
                data.AsSpan(offset, size).CopyTo(pipe.Writer.GetSpan(size));
                pipe.Writer.Advance(size);
                Interlocked.Add(ref written, size);

                ValueTask<FlushResult> flush = pipe.Writer.FlushAsync();
                if (!flush.IsCompleted) pauses++;
                maxBuffered = Math.Max(maxBuffered, Interlocked.Read(ref written) - Interlocked.Read(ref consumed));
                await flush;
            }
            await pipe.Writer.CompleteAsync();
        });

        // A slow consumer: 1 ms of "work" per 500 orders.
        long orders = 0;
        long count = await NdjsonOrders.ReadWithPipeReaderAsync(new CountingPipeReader(pipe.Reader, n => Interlocked.Add(ref consumed, n)),
            async _ =>
            {
                if (++orders % 500 == 0) await Task.Delay(1);
            });
        await writer;

        Console.WriteLine($"{count:N0} orders, {data.Length:N0} bytes");
        Console.WriteLine($"Writer paused {pauses} times, max buffered {maxBuffered:N0} bytes");
    }

    public static async Task JsonPipeAsync()
    {
        byte[] data = CreateNdjson(20_000);
        var reader = PipeReader.Create(new MemoryStream(data));
        long count = 0;
        decimal total = 0;
        await foreach (var order in JsonSerializer.DeserializeAsyncEnumerable<Order>(reader, topLevelValues: true))
        {
            count++;
            total += order!.Total;
        }
        Console.WriteLine($"DeserializeAsyncEnumerable(PipeReader): {count:N0} orders, {total:N2} TRY");
    }
}
