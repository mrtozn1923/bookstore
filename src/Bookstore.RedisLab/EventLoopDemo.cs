using System.Diagnostics;
using StackExchange.Redis;

namespace Bookstore.RedisLab;

public static class EventLoopDemo
{
    public static async Task SeedKeysAsync(string connectionString, int count)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString);
        IDatabase db = mux.GetDatabase();
        Stopwatch sw = Stopwatch.StartNew();
        const int batchSize = 10_000;
        for (int start = 1; start <= count; start += batchSize)
        {
            int end = Math.Min(start + batchSize - 1, count);
            Task[] writes = [.. Enumerable.Range(start, end - start + 1)
                .Select(id => db.StringSetAsync($"bookstore:book:{id}:views", id % 500))];
            await Task.WhenAll(writes);
        }
        Console.WriteLine($"{count:N0} keys written in {sw.ElapsedMilliseconds:N0} ms");
    }

    public static async Task KeysVersusPingAsync(string connectionString)
    {
        // Two separate multiplexers = two separate application instances.
        await using ConnectionMultiplexer instanceA = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=instance-a");
        // KEYS is an admin command for StackExchange.Redis 3.x; instance B is an "ops script"
        await using ConnectionMultiplexer instanceB = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=ops-script,allowAdmin=true");
        IDatabase dbA = instanceA.GetDatabase();
        IDatabase dbB = instanceB.GetDatabase();

        List<double> pings = [];
        Task pinger = Task.Run(async () =>
        {
            for (int i = 0; i < 300; i++)
            {
                TimeSpan rtt = await dbA.PingAsync();
                pings.Add(rtt.TotalMilliseconds);
                await Task.Delay(10);
            }
        });

        await Task.Delay(1000);
        Stopwatch sw = Stopwatch.StartNew();
        RedisResult keys = await dbB.ExecuteAsync("KEYS", "bookstore:book:*");
        Console.WriteLine($"instance B: KEYS returned {keys.Length:N0} keys in {sw.ElapsedMilliseconds:N0} ms");

        await pinger;
        double[] sorted = [.. pings.Order()];
        Console.WriteLine($"instance A: {pings.Count} PINGs, median {sorted[sorted.Length / 2]:F2} ms, " +
                          $"p99 {sorted[(int)(sorted.Length * 0.99)]:F2} ms, max {sorted[^1]:F2} ms");
    }

    public static async Task ParallelIncrementAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString);
        IDatabase db = mux.GetDatabase();
        const string key = "bookstore:book:42:views";
        await db.KeyDeleteAsync(key);

        // 100 concurrent callers, each increments 1,000 times; no lock on the client side
        Task[] callers = [.. Enumerable.Range(0, 100).Select(_ => Task.Run(async () =>
        {
            for (int i = 0; i < 1_000; i++)
            {
                await db.StringIncrementAsync(key);
            }
        }))];
        await Task.WhenAll(callers);

        Console.WriteLine($"{key} = {await db.StringGetAsync(key)}");
    }

    public static async Task RoundTripAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",allowAdmin=true");
        IDatabase db = mux.GetDatabase();
        IServer server = mux.GetServers()[0];

        await db.StringSetAsync("bookstore:book:42:title", "Dune");
        await db.StringSetAsync("bookstore:report:big", new string('x', 4 * 1024 * 1024));

        await MeasureAsync("GET small (4 bytes)", "bookstore:book:42:title", 10_000);
        await MeasureAsync("GET big (4 MB)", "bookstore:report:big", 50);

        async Task MeasureAsync(string label, string key, int count)
        {
            await server.ConfigResetStatisticsAsync();
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                await db.StringGetAsync(key); // one round trip at a time
            }
            double clientMicros = sw.Elapsed.TotalMicroseconds / count;
            string serverMicros = await GetUsecPerCallAsync(server, "get");
            Console.WriteLine($"{label,-20} client: {clientMicros,8:F0} us/call   server (commandstats): {serverMicros} us/call");
        }
    }

    private static async Task<string> GetUsecPerCallAsync(IServer server, string command)
    {
        IGrouping<string, KeyValuePair<string, string>>[] info = await server.InfoAsync("commandstats");
        string line = info.SelectMany(g => g).First(kv => kv.Key == $"cmdstat_{command}").Value;
        // calls=10000,usec=11234,usec_per_call=1.12,...
        return line.Split(',').First(p => p.StartsWith("usec_per_call=")).Split('=')[1];
    }
}
