using System.Diagnostics;
using StackExchange.Redis;

namespace Bookstore.RedisLab;

public static class StreamsDemo
{
    private const string Stream = "bookstore:orders:events";

    private static NameValueEntry[] OrderCreated(long orderId) =>
        [new("type", "order-created"), new("orderId", orderId), new("customerId", 7), new("isbn", "9780441013593"), new("quantity", 1)];

    // A consumer reads three events, acknowledges one and dies; a second consumer takes over the rest
    public static async Task CrashAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=email-worker");
        IDatabase db = mux.GetDatabase();
        await db.KeyDeleteAsync(Stream);
        for (long id = 1001; id <= 1003; id++)
        {
            await db.StreamAddAsync(Stream, OrderCreated(id));
        }
        await db.StreamCreateConsumerGroupAsync(Stream, "emails", StreamPosition.Beginning);

        StreamEntry[] first = await db.StreamReadGroupAsync(Stream, "emails", "emails-worker-1", StreamPosition.NewMessages, count: 10);
        await db.StreamAcknowledgeAsync(Stream, "emails", first[0].Id);
        Console.WriteLine($"emails-worker-1 read {first.Length}, acknowledged {first[0]["orderId"]}, then crashed");

        StreamPendingInfo pending = await db.StreamPendingAsync(Stream, "emails");
        Console.WriteLine($"XPENDING: {pending.PendingMessageCount} pending, " +
                          string.Join(", ", pending.Consumers.Select(c => $"{c.Name}={c.PendingMessageCount}")));

        await db.StreamAddAsync(Stream, OrderCreated(1004));
        await Task.Delay(1100);
        // XREADGROUP ... CLAIM 1000: idle pending entries first (with idle time and delivery count), then new ones
        StreamEntry[] second = await db.StreamReadGroupAsync(Stream, "emails", "emails-worker-2", StreamPosition.NewMessages,
            count: 10, noAck: false, claimMinIdleTime: TimeSpan.FromSeconds(1));
        foreach (StreamEntry entry in second)
        {
            Console.WriteLine($"emails-worker-2 got order {entry["orderId"]}: idle={entry.IdleTime?.TotalMilliseconds:F0} ms, " +
                              $"delivered before={entry.DeliveryCount}");
            await db.StreamAcknowledgeAsync(Stream, "emails", entry.Id);
        }
        Console.WriteLine($"XPENDING after: {(await db.StreamPendingAsync(Stream, "emails")).PendingMessageCount}");
    }

    // The same XADD sent twice, as a producer would after a timeout
    public static async Task IdempotentAddAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=bookstore-api");
        IDatabase db = mux.GetDatabase();
        await db.KeyDeleteAsync(Stream);

        foreach (bool idempotent in new[] { false, true })
        {
            for (int attempt = 1; attempt <= 2; attempt++)
            {
                RedisValue id = idempotent
                    ? await db.StreamAddAsync(Stream, OrderCreated(1005), new StreamIdempotentId("bookstore-api", "7f3c9a"))
                    : await db.StreamAddAsync(Stream, OrderCreated(1005));
                Console.WriteLine($"{(idempotent ? "IDMP" : "plain"),-5} attempt {attempt}: {id}");
            }
        }
        Console.WriteLine($"XLEN: {await db.StreamLengthAsync(Stream)}");
        RedisResult info = await db.ExecuteAsync("XINFO", "STREAM", Stream);
        Dictionary<string, RedisResult> fields = info.ToDictionary();
        Console.WriteLine(string.Join(", ", new[] { "idmp-duration", "idmp-maxsize", "pids-tracked", "iids-tracked", "iids-added", "iids-duplicates" }
            .Select(f => $"{f}={fields[f]}")));
    }

    // One million order events: memory, and exact versus approximate trimming
    public static async Task MemoryAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=observer");
        IDatabase db = mux.GetDatabase();
        RedisKey key = "bookstore:orders:events:load";
        await db.KeyDeleteAsync(key);

        for (int batch = 0; batch < 100; batch++)
        {
            await Task.WhenAll(Enumerable.Range(0, 10_000).Select(i => db.StreamAddAsync(key, OrderCreated(batch * 10_000 + i))));
        }
        long bytes = (long)await db.ExecuteAsync("MEMORY", "USAGE", key, "SAMPLES", "0");
        Console.WriteLine($"1,000,000 events: {bytes:N0} bytes ({bytes / 1_000_000.0:F1} bytes per event)");

        Stopwatch sw = Stopwatch.StartNew();
        long approx = await db.StreamTrimAsync(key, 900_000, useApproximateMaxLength: true);   // XTRIM key MAXLEN ~ 900000
        Console.WriteLine($"XTRIM MAXLEN ~ 900000:        removed {approx:N0}, length {await db.StreamLengthAsync(key):N0} ({sw.ElapsedMilliseconds} ms)");
        sw.Restart();
        long exact = await db.StreamTrimAsync(key, 800_000, useApproximateMaxLength: false);   // XTRIM key MAXLEN 800000
        Console.WriteLine($"XTRIM MAXLEN 800000:          removed {exact:N0}, length {await db.StreamLengthAsync(key):N0} ({sw.ElapsedMilliseconds} ms)");

        sw.Restart();
        // StreamTrimAsync rejects limit: 0 on the client side, so the command is sent as is
        long unlimited = (long)await db.ExecuteAsync("XTRIM", key, "MAXLEN", "~", 700_000, "LIMIT", 0);
        Console.WriteLine($"XTRIM MAXLEN ~ 700000 LIMIT 0: removed {unlimited:N0}, length {await db.StreamLengthAsync(key):N0} ({sw.ElapsedMilliseconds} ms)");
        await db.KeyDeleteAsync(key);
    }
}
