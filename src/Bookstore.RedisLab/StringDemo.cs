using System.Diagnostics;
using StackExchange.Redis;

namespace Bookstore.RedisLab;

public static class StringDemo
{
    // Read-modify-write on the client: GET, add one, SET
    public static async Task LostUpdateAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=bookstore-api");
        IDatabase db = mux.GetDatabase();
        const string key = "bookstore:book:42:views";
        await db.StringSetAsync(key, 0);

        Task[] callers = [.. Enumerable.Range(0, 50).Select(_ => Task.Run(async () =>
        {
            for (int i = 0; i < 200; i++)
            {
                long current = (long)await db.StringGetAsync(key);
                await db.StringSetAsync(key, current + 1);
            }
        }))];
        await Task.WhenAll(callers);

        Console.WriteLine($"expected 10000, got {await db.StringGetAsync(key)}");
    }

    // 200,000 keys that expire within 1-2 seconds and are never read again
    public static async Task ActiveExpireAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=observer,allowAdmin=true");
        IDatabase db = mux.GetDatabase();
        IServer server = mux.GetServers()[0];
        await server.FlushDatabaseAsync();
        await server.ConfigResetStatisticsAsync();

        for (int start = 0; start < 200_000; start += 10_000)
        {
            await Task.WhenAll(Enumerable.Range(start, 10_000).Select(i =>
                db.StringSetAsync($"bookstore:verify:{i}", "482913", TimeSpan.FromMilliseconds(1000 + i % 1000))));
        }

        Stopwatch sw = Stopwatch.StartNew();
        for (int second = 0; second <= 6; second++)
        {
            long keys = await server.DatabaseSizeAsync();
            string expired = await GetInfoAsync(server, "stats", "expired_keys");
            Console.WriteLine($"t={sw.Elapsed.TotalSeconds,4:F1}s  DBSIZE={keys,7:N0}  expired_keys={long.Parse(expired),7:N0}");
            await Task.Delay(1000);
        }
    }

    // A 60-second window counter: EXPIRE on every INCR versus EXPIRE ... NX
    public static async Task TtlResetAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=bookstore-api");
        IDatabase db = mux.GetDatabase();
        RedisKey always = "bookstore:rl:7:always", once = "bookstore:rl:7:once";
        await db.KeyDeleteAsync([always, once]);

        for (int i = 0; i < 4; i++)
        {
            long a = await db.StringIncrementAsync(always);
            await db.KeyExpireAsync(always, TimeSpan.FromSeconds(60)); // resets the window every time
            long b = await db.StringIncrementAsync(once);
            await db.KeyExpireAsync(once, TimeSpan.FromSeconds(60), ExpireWhen.HasNoExpiry); // EXPIRE ... NX
            Console.WriteLine($"t={i * 2}s  always: count={a} ttl={(await db.KeyTimeToLiveAsync(always))?.TotalSeconds:F0}" +
                              $"   NX: count={b} ttl={(await db.KeyTimeToLiveAsync(once))?.TotalSeconds:F0}");
            await Task.Delay(2000);
        }
    }

    private static async Task<string> GetInfoAsync(IServer server, string section, string name)
    {
        IGrouping<string, KeyValuePair<string, string>>[] info = await server.InfoAsync(section);
        return info.SelectMany(g => g).First(kv => kv.Key == name).Value;
    }
}
