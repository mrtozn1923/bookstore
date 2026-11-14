using System.Diagnostics;
using System.Text.Json;
using StackExchange.Redis;

namespace Bookstore.RedisLab;

public static class HashDemo
{
    private static readonly string[] Isbns =
        ["9780441013593", "9780451524935", "9780141439518", "9780547928227", "9780141439549"];

    // 10,000 carts with 5 items each, stored three different ways
    public static async Task CartStorageAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=observer,allowAdmin=true");
        IDatabase db = mux.GetDatabase();
        IServer server = mux.GetServers()[0];

        await MeasureAsync("hash per cart", customerId =>
            db.HashSetAsync($"bookstore:cart:{customerId}", [.. Isbns.Select((isbn, i) => new HashEntry(isbn, i + 1))]));

        await MeasureAsync("JSON string per cart", customerId =>
            db.StringSetAsync($"bookstore:cart:{customerId}",
                JsonSerializer.Serialize(Isbns.Select((isbn, i) => new { isbn, quantity = i + 1 }))));

        await MeasureAsync("key per cart item", customerId =>
            Task.WhenAll(Isbns.Select((isbn, i) => db.StringSetAsync($"bookstore:cart:{customerId}:{isbn}", i + 1))));

        async Task MeasureAsync(string label, Func<int, Task> write)
        {
            await server.FlushDatabaseAsync();
            long before = await UsedMemoryAsync(server);
            await Task.WhenAll(Enumerable.Range(1, 10_000).Select(write));
            long after = await UsedMemoryAsync(server);
            Console.WriteLine($"{label,-22} keys={await server.DatabaseSizeAsync(),6:N0}  memory={(after - before) / 1024.0 / 1024.0,5:F2} MB");
        }
    }

    // When does a hash leave listpack, and what does it cost?
    public static async Task EncodingAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=observer");
        IDatabase db = mux.GetDatabase();
        RedisKey key = "bookstore:customer:7:wishlist";

        foreach (int fields in new[] { 128, 512, 513 })
        {
            await db.KeyDeleteAsync(key);
            await db.HashSetAsync(key, [.. Enumerable.Range(0, fields).Select(i => new HashEntry($"978{i:D10}", DateTime.UnixEpoch.Ticks))]);
            await PrintAsync($"{fields} fields");
        }

        foreach (int length in new[] { 64, 65 })
        {
            await db.KeyDeleteAsync(key);
            await db.HashSetAsync(key, [new HashEntry("note", new string('x', length))]);
            await PrintAsync($"1 field, value {length} bytes");
        }

        async Task PrintAsync(string label)
        {
            RedisResult encoding = await db.ExecuteAsync("OBJECT", "ENCODING", key);
            RedisResult usage = await db.ExecuteAsync("MEMORY", "USAGE", key);
            Console.WriteLine($"{label,-26} encoding={encoding,-10} memory={(long)usage,6:N0} bytes");
        }
    }

    // HGETALL on a hash with one million fields versus HSCAN
    public static async Task BigHashAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=bookstore-api,allowAdmin=true");
        IDatabase db = mux.GetDatabase();
        IServer server = mux.GetServers()[0];
        RedisKey key = "bookstore:report:views-by-book";
        await db.KeyDeleteAsync(key);
        for (int start = 0; start < 1_000_000; start += 10_000)
        {
            await db.HashSetAsync(key, [.. Enumerable.Range(start, 10_000).Select(i => new HashEntry(i, i % 500))]);
        }
        await server.SlowlogResetAsync();

        Stopwatch sw = Stopwatch.StartNew();
        HashEntry[] all = await db.HashGetAllAsync(key);
        Console.WriteLine($"HGETALL: {all.Length:N0} fields in {sw.ElapsedMilliseconds:N0} ms");

        sw.Restart();
        long count = 0;
        await foreach (HashEntry _ in db.HashScanAsync(key, pageSize: 1000))
        {
            count++;
        }
        Console.WriteLine($"HSCAN:   {count:N0} fields in {sw.ElapsedMilliseconds:N0} ms");

        foreach (CommandTrace trace in await server.SlowlogGetAsync(10))
        {
            Console.WriteLine($"slowlog: {trace.Arguments[0]} {trace.Duration.TotalMilliseconds:F1} ms");
        }
        await db.KeyDeleteAsync(key);
    }

    // 100,000 books with the same four fields: plain HSET versus an 8.10 compact hash (HIMPORT)
    public static async Task CompactHashAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=observer,allowAdmin=true");
        IDatabase db = mux.GetDatabase();
        IServer server = mux.GetServers()[0];
        RedisValue[] fields = ["title", "author", "price", "stock"];

        await server.FlushDatabaseAsync();
        long before = await UsedMemoryAsync(server);
        await Task.WhenAll(Enumerable.Range(1, 100_000).Select(id => db.HashSetAsync($"bookstore:book:{id}",
            [new HashEntry("title", $"Dune #{id}"), new HashEntry("author", "Frank Herbert"), new HashEntry("price", 34990), new HashEntry("stock", id % 40)])));
        Console.WriteLine($"HSET     memory={(await UsedMemoryAsync(server) - before) / 1024.0 / 1024.0,5:F2} MB");

        await server.FlushDatabaseAsync();
        before = await UsedMemoryAsync(server);
        using HashImport bookFields = HashImport.Create(fields);
        await Task.WhenAll(Enumerable.Range(1, 100_000).Select(id => db.HashImportAsync($"bookstore:book:{id}", bookFields,
            new RedisValue[] { $"Dune #{id}", "Frank Herbert", 34990, id % 40 })));
        Console.WriteLine($"HIMPORT  memory={(await UsedMemoryAsync(server) - before) / 1024.0 / 1024.0,5:F2} MB, " +
                          $"encoding={await db.ExecuteAsync("OBJECT", "ENCODING", "bookstore:book:1")}");
        await server.FlushDatabaseAsync();
    }

    private static async Task<long> UsedMemoryAsync(IServer server)
    {
        IGrouping<string, KeyValuePair<string, string>>[] info = await server.InfoAsync("memory");
        return long.Parse(info.SelectMany(g => g).First(kv => kv.Key == "used_memory").Value);
    }
}
