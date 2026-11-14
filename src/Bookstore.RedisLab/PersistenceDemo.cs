using System.Diagnostics;
using StackExchange.Redis;

namespace Bookstore.RedisLab;

public static class PersistenceDemo
{
    private const string Stream = "bookstore:orders:events";
    private const string Sequence = "bookstore:order:id-seq";

    // Writes order events and order ids, as the API would; the container is killed right after
    public static async Task WriteOrdersAsync(string connectionString, int count)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString);
        IDatabase db = mux.GetDatabase();
        List<Task> pending = [];
        for (int i = 0; i < count; i++)
        {
            pending.Add(db.StringIncrementAsync(Sequence));
            pending.Add(db.StreamAddAsync(Stream, [new("type", "order-created"), new("isbn", "9780441013593"), new("quantity", 1)]));
        }
        await Task.WhenAll(pending);
        Console.WriteLine($"wrote {count:N0} orders, {Sequence} = {await db.StringGetAsync(Sequence)}, XLEN = {await db.StreamLengthAsync(Stream)}");
    }

    // After a restart: what survived?
    public static async Task CheckOrdersAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString);
        IDatabase db = mux.GetDatabase();
        RedisValue seq = await db.StringGetAsync(Sequence);
        Console.WriteLine($"{Sequence} = {(seq.IsNull ? "(nil)" : seq)}, XLEN = {await db.StreamLengthAsync(Stream)}");
    }

    // BGSAVE while the application keeps writing: fork time and copy-on-write memory
    public static async Task ForkCostAsync(string connectionString)
    {
        const int keys = 2_000_000;
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",allowAdmin=true");
        IDatabase db = mux.GetDatabase();
        IServer server = mux.GetServers()[0];
        await server.FlushDatabaseAsync();
        string value = new('x', 200);
        foreach (int[] chunk in Enumerable.Range(0, keys).Chunk(10_000))
        {
            await Task.WhenAll(chunk.Select(i => db.StringSetAsync($"bookstore:session:{i}", value)));
        }
        Console.WriteLine($"{keys:N0} keys, used_memory {await InfoAsync(server, "memory", "used_memory_human")}");

        foreach (int percent in new[] { 0, 50 })
        {
            await server.SaveAsync(SaveType.BackgroundSave);
            Stopwatch sw = Stopwatch.StartNew();
            // While the child writes the snapshot, the parent keeps changing pages
            int toWrite = keys * percent / 100;
            foreach (int[] chunk in Enumerable.Range(0, toWrite).Chunk(10_000))
            {
                await Task.WhenAll(chunk.Select(i => db.StringSetAsync($"bookstore:session:{i}", value)));
            }
            while (await InfoAsync(server, "persistence", "rdb_bgsave_in_progress") == "1")
            {
                await Task.Delay(50);
            }
            Console.WriteLine($"BGSAVE with {percent}% of keys rewritten: latest_fork_usec {await InfoAsync(server, "stats", "latest_fork_usec")}, " +
                              $"rdb_last_cow_size {long.Parse(await InfoAsync(server, "persistence", "rdb_last_cow_size")) / 1048576} MB, " +
                              $"took {sw.ElapsedMilliseconds:N0} ms");
        }
    }

    // A stream and cache keys under a small maxmemory: allkeys-lru vs noeviction
    public static async Task EvictionAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",allowAdmin=true");
        IDatabase db = mux.GetDatabase();
        IServer server = mux.GetServers()[0];
        string value = new('x', 1000);
        foreach (string policy in new[] { "allkeys-lru", "noeviction" })
        {
            await server.FlushDatabaseAsync();
            await server.ConfigSetAsync("maxmemory", "20mb");
            await server.ConfigSetAsync("maxmemory-policy", policy);
            await server.ExecuteAsync("CONFIG", "RESETSTAT");
            for (int i = 0; i < 1_000; i++)
            {
                await db.StreamAddAsync(Stream, [new("orderId", i), new("isbn", "9780441013593")]);
            }
            int written = 0;
            string? error = null;
            try
            {
                for (; written < 50_000; written++)
                {
                    await db.StringSetAsync($"bookstore:cache:book:{written}", value);
                }
            }
            catch (RedisServerException ex)
            {
                error = ex.Message;
            }
            Console.WriteLine($"{policy}: cache keys written {written:N0}, evicted_keys {await InfoAsync(server, "stats", "evicted_keys")}, " +
                              $"stream exists {await db.KeyExistsAsync(Stream)}");
            if (error is not null)
            {
                Console.WriteLine($"  {error}");
                Console.WriteLine($"  GET still works: {(await db.StringGetAsync("bookstore:cache:book:1")).HasValue}, " +
                                  $"DEL works: {await db.KeyDeleteAsync("bookstore:cache:book:1")}");
            }
        }
        await server.ConfigSetAsync("maxmemory", "0");
    }

    // DEL vs UNLINK on a set with 1,000,000 members while another client sends PING
    public static async Task BigKeyDeleteAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",allowAdmin=true");
        await using ConnectionMultiplexer probe = await ConnectionMultiplexer.ConnectAsync(connectionString);
        IDatabase db = mux.GetDatabase();
        IServer server = mux.GetServers()[0];
        foreach (string command in new[] { "DEL", "UNLINK" })
        {
            RedisKey key = "bookstore:visitors:all";
            foreach (int[] chunk in Enumerable.Range(0, 1_000_000).Chunk(10_000))
            {
                await db.SetAddAsync(key, [.. chunk.Select(i => (RedisValue)$"customer-{i}")]);
            }
            await server.SlowlogResetAsync();
            using CancellationTokenSource stop = new();
            Task<double> pings = MaxPingAsync(probe.GetDatabase(), stop.Token);
            await Task.Delay(100);
            Stopwatch sw = Stopwatch.StartNew();
            await db.ExecuteAsync(command, key);
            double client = sw.Elapsed.TotalMilliseconds;
            await Task.Delay(100);
            stop.Cancel();
            CommandTrace[] slow = await server.SlowlogGetAsync(10);
            CommandTrace? trace = slow.FirstOrDefault(t => (string?)t.Arguments[0] == command);
            Console.WriteLine($"{command,-6} client {client,6:F1} ms, slowlog {(trace is null ? "-" : $"{trace.Duration.TotalMilliseconds:F1} ms")}, " +
                              $"max PING meanwhile {await pings:F1} ms");
        }
    }

    // Fills memory with 1 MB values until the server (or the container) gives up
    public static async Task FillAsync(string connectionString, int megabytes)
    {
        ConfigurationOptions options = ConfigurationOptions.Parse(connectionString);
        options.AbortOnConnectFail = false;
        options.AllowAdmin = true;
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(options);
        IDatabase db = mux.GetDatabase();
        IServer server = mux.GetServers()[0];
        string value = new('x', 1024 * 1024);
        for (int i = 1; i <= megabytes; i++)
        {
            try
            {
                await db.StringSetAsync($"bookstore:report:{i}", value);
            }
            catch (Exception ex) when (ex is RedisException or TimeoutException)
            {
                Console.WriteLine($"after {i - 1} MB: {ex.GetType().Name}: {ex.Message.Split(',')[0]}");
                return;
            }
            if (i % 32 == 0)
            {
                Console.WriteLine($"{i} MB written, used_memory {await InfoAsync(server, "memory", "used_memory_human")}");
            }
        }
    }

    private static async Task<double> MaxPingAsync(IDatabase db, CancellationToken token)
    {
        double max = 0;
        while (!token.IsCancellationRequested)
        {
            max = Math.Max(max, (await db.PingAsync()).TotalMilliseconds);
        }
        return max;
    }

    private static async Task<string> InfoAsync(IServer server, string section, string key) =>
        (await server.InfoAsync(section)).SelectMany(g => g).First(p => p.Key == key).Value;
}
