using System.Diagnostics;
using StackExchange.Redis;

namespace Bookstore.RedisLab;

public static class ReplicationDemo
{
    // Writes an order id every 100 ms through Sentinel while the primary is killed from another terminal
    public static async Task SentinelFailoverAsync(int seconds)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(
            "localhost:26379,localhost:26380,localhost:26381,serviceName=bookstore,abortConnect=false");
        Stopwatch clock = Stopwatch.StartNew();
        void Log(string message) => Console.WriteLine($"{clock.Elapsed.TotalSeconds,6:F1}s {message}");
        mux.ConnectionFailed += (_, e) => Log($"event ConnectionFailed {e.EndPoint} {e.ConnectionType} {e.FailureType}");
        mux.ConnectionRestored += (_, e) => Log($"event ConnectionRestored {e.EndPoint} {e.ConnectionType}");

        await RunWritesAsync(mux, seconds, i => (RedisKey)"bookstore:order:id-seq", Log);
    }

    // Slot of each key, which node owns it, and what is refused across slots
    public static async Task ClusterKeysAsync()
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync("127.0.0.1:7000,127.0.0.1:7001,127.0.0.1:7002");
        IDatabase db = mux.GetDatabase();
        foreach (IServer server in mux.GetServers().OrderBy(s => s.EndPoint.ToString()))
        {
            Console.WriteLine($"{server.EndPoint} {(server.IsReplica ? "replica" : "primary")}");
        }

        RedisKey stock = "bookstore:stock:{9780441013593}";
        RedisKey reservations = "bookstore:stock:{9780441013593}:reservations";
        RedisKey cart = "bookstore:cart:7";
        foreach (RedisKey key in new[] { stock, reservations, cart })
        {
            Console.WriteLine($"{key,-46} slot {mux.HashSlot(key),5} on {await db.IdentifyEndpointAsync(key)}");
        }

        await db.StringSetAsync(stock, 3);
        RedisResult reserved = await db.ScriptEvaluateAsync(
            "redis.call('HSET', KEYS[2], ARGV[1], ARGV[2]) return redis.call('DECRBY', KEYS[1], ARGV[2])",
            [stock, reservations], ["order-1001", 2]);
        Console.WriteLine($"script on {{9780441013593}} keys: stock left {reserved}");

        try
        {
            await db.StringSetAsync([new(stock, 10), new(cart, "x")]);                 // MSET over two slots
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MSET stock + cart: {ex.GetType().Name}: {ex.Message}");
        }
        try
        {
            await db.ScriptEvaluateAsync("return redis.call('GET', KEYS[1])", [stock, cart]);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"EVAL stock + cart: {ex.GetType().Name}: {ex.Message}");
        }
    }

    // Keyspace notifications are node-local: one subscription sees the expiries of one node only
    public static async Task ClusterNotificationsAsync()
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync("127.0.0.1:7000,127.0.0.1:7001,127.0.0.1:7002,allowAdmin=true");
        IServer[] primaries = [.. mux.GetServers().Where(s => !s.IsReplica)];
        foreach (IServer server in primaries)
        {
            await server.ConfigSetAsync("notify-keyspace-events", "Ex");
        }
        int received = 0;
        ISubscriber sub = mux.GetSubscriber();
        await sub.SubscribeAsync(RedisChannel.Literal("__keyevent@0__:expired"), (_, _) => Interlocked.Increment(ref received));

        IDatabase db = mux.GetDatabase();
        const int count = 300;
        await Task.WhenAll(Enumerable.Range(0, count).Select(i =>
            db.StringSetAsync($"bookstore:verify:{i}", "482913", TimeSpan.FromMilliseconds(500))));
        int[] perNode = [.. primaries.Select(p => Enumerable.Range(0, count).Count(i =>
            db.IdentifyEndpoint($"bookstore:verify:{i}")!.Equals(p.EndPoint)))];
        await Task.Delay(3000);
        Console.WriteLine($"{count} keys expired ({string.Join(" + ", perNode)} per primary), notifications received: {received}");
    }

    // Writes 50 keys spread over all slots every 100 ms while a primary is killed from another terminal
    public static async Task ClusterFailoverAsync(int seconds)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync("127.0.0.1:7000,127.0.0.1:7001,127.0.0.1:7002");
        Stopwatch clock = Stopwatch.StartNew();
        void Log(string message) => Console.WriteLine($"{clock.Elapsed.TotalSeconds,6:F1}s {message}");
        await RunWritesAsync(mux, seconds, i => (RedisKey)$"bookstore:cart:{i}", Log, keysPerRound: 50);
    }

    private static async Task RunWritesAsync(ConnectionMultiplexer mux, int seconds, Func<int, RedisKey> keyFor,
        Action<string> log, int keysPerRound = 1)
    {
        IDatabase db = mux.GetDatabase();
        Stopwatch clock = Stopwatch.StartNew();
        int ok = 0, failed = 0;
        bool failing = false;
        while (clock.Elapsed < TimeSpan.FromSeconds(seconds))
        {
            Task[] round = [.. Enumerable.Range(0, keysPerRound).Select(i => db.StringIncrementAsync(keyFor(i)))];
            try
            {
                await Task.WhenAll(round);
                ok += keysPerRound;
                if (failing)
                {
                    log($"writes succeed again (primary for slot of {keyFor(0)}: {await db.IdentifyEndpointAsync(keyFor(0))})");
                    failing = false;
                }
            }
            catch (Exception ex)
            {
                int bad = round.Count(t => t.IsFaulted);
                failed += bad;
                ok += keysPerRound - bad;
                if (!failing)
                {
                    log($"first failure: {ex.GetType().Name}: {ex.Message.Split(',')[0]}");
                    failing = true;
                }
            }
            await Task.Delay(100);
        }
        log($"done: {ok} writes ok, {failed} failed");
    }
}
