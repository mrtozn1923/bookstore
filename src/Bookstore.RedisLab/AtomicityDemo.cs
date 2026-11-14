using System.Diagnostics;
using StackExchange.Redis;

namespace Bookstore.RedisLab;

public static class AtomicityDemo
{
    private const string StockKey = "bookstore:stock:{9780441013593}";

    // Reserve one copy if stock >= 1, in one step on the server
    private const string ReserveScript = """
        local stock = tonumber(redis.call('GET', KEYS[1]) or '0')
        if stock < tonumber(ARGV[1]) then
            return -1
        end
        return redis.call('DECRBY', KEYS[1], ARGV[1])
        """;

    // 200 customers try to buy the last 100 copies at the same time, three ways
    public static async Task StockRaceAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=bookstore-api");
        IDatabase db = mux.GetDatabase();

        foreach (string mode in new[] { "check-then-act", "transaction", "lua" })
        {
            await db.StringSetAsync(StockKey, 100);
            int retries = 0;
            Stopwatch sw = Stopwatch.StartNew();
            Task<bool>[] buyers = [.. Enumerable.Range(0, 200).Select(_ => Task.Run(async () =>
            {
                switch (mode)
                {
                    case "check-then-act":
                        long stock = (long)await db.StringGetAsync(StockKey);              // read ...
                        if (stock < 1)
                        {
                            return false;
                        }
                        await db.StringDecrementAsync(StockKey);                            // ... then write
                        return true;
                    case "transaction":
                        while (true)
                        {
                            RedisValue current = await db.StringGetAsync(StockKey);
                            if ((long)current < 1)
                            {
                                return false;
                            }
                            ITransaction tx = db.CreateTransaction();
                            tx.AddCondition(Condition.StringEqual(StockKey, current));     // WATCH + check
                            Task<long> decremented = tx.StringDecrementAsync(StockKey);     // queued, not sent
                            if (await tx.ExecuteAsync())                                    // MULTI ... EXEC
                            {
                                await decremented;
                                return true;
                            }
                            Interlocked.Increment(ref retries);                             // someone else changed it
                        }
                    default:
                        RedisResult left = await db.ScriptEvaluateAsync(ReserveScript, [StockKey], [1]);
                        return (long)left >= 0;
                }
            }))];
            bool[] results = await Task.WhenAll(buyers);
            Console.WriteLine($"{mode,-15} sold {results.Count(r => r),3}  stock left {(long)await db.StringGetAsync(StockKey),4}  " +
                              $"retries {retries,4}  {sw.ElapsedMilliseconds,4} ms");
        }
    }

    // EVALSHA, the script cache and what SCRIPT FLUSH (or a restart) does to it
    public static async Task ScriptCacheAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=observer,allowAdmin=true");
        IDatabase db = mux.GetDatabase();
        IServer server = mux.GetServers()[0];
        await db.StringSetAsync(StockKey, 5);

        LuaScript script = LuaScript.Prepare("""
            local stock = tonumber(redis.call('GET', @stockKey) or '0')
            if stock < tonumber(@quantity) then
                return -1
            end
            return redis.call('DECRBY', @stockKey, @quantity)
            """);
        LoadedLuaScript loaded = await script.LoadAsync(server);                                    // SCRIPT LOAD
        Console.WriteLine($"sha1: {Convert.ToHexStringLower(loaded.Hash)}");
        Console.WriteLine($"EVALSHA: {await loaded.EvaluateAsync(db, new { stockKey = (RedisKey)StockKey, quantity = 2 })}");
        Console.WriteLine($"cached on server: {await server.ScriptExistsAsync(loaded.Hash)}");

        await server.ScriptFlushAsync();                                                           // what a restart also does
        Console.WriteLine($"after SCRIPT FLUSH, cached: {await server.ScriptExistsAsync(loaded.Hash)}");
        try
        {
            Console.WriteLine($"LoadedLuaScript: {await loaded.EvaluateAsync(db, new { stockKey = (RedisKey)StockKey, quantity = 2 })}");
        }
        catch (RedisServerException ex)
        {
            Console.WriteLine($"LoadedLuaScript: {ex.Message}");
        }
        Console.WriteLine($"LuaScript:       {await script.EvaluateAsync(db, new { stockKey = (RedisKey)StockKey, quantity = 2 })}");
    }

    // A script that runs for 8 seconds: what the other clients see, and SCRIPT KILL
    public static async Task BusyScriptAsync(string connectionString)
    {
        await using ConnectionMultiplexer worker = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=report-job,syncTimeout=20000,asyncTimeout=20000");
        await using ConnectionMultiplexer api = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=bookstore-api,allowAdmin=true");
        IDatabase apiDb = api.GetDatabase();

        Stopwatch sw = Stopwatch.StartNew();
        Task<RedisResult> busy = worker.GetDatabase().ScriptEvaluateAsync(
            "local t = redis.call('TIME') local stop = t[1] + 8 while redis.call('TIME')[1] + 0 < stop do end return 'done'");
        await Task.Delay(500);
        try
        {
            await apiDb.PingAsync();
            Console.WriteLine($"{sw.Elapsed.TotalSeconds,4:F1}s PING ok");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{sw.Elapsed.TotalSeconds,4:F1}s PING failed: {ex.GetType().Name}: {ex.Message}");
        }
        await Task.Delay(500);
        RedisResult kill = await apiDb.ExecuteAsync("SCRIPT", "KILL");
        Console.WriteLine($"{sw.Elapsed.TotalSeconds,4:F1}s SCRIPT KILL: {kill}");
        try
        {
            Console.WriteLine($"script: {await busy}");
        }
        catch (RedisServerException ex)
        {
            Console.WriteLine($"{sw.Elapsed.TotalSeconds,4:F1}s script: {ex.Message}");
        }
        Console.WriteLine($"{sw.Elapsed.TotalSeconds,4:F1}s PING after kill: {(await apiDb.PingAsync()).TotalMilliseconds:F1} ms");
    }

    // A Redis Function, loaded once at deploy time, called by name
    public static async Task FunctionCallAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=bookstore-api");
        IDatabase db = mux.GetDatabase();
        RedisKey reservations = "bookstore:stock:{9780441013593}:reservations";
        await db.StringSetAsync(StockKey, 3);
        await db.KeyDeleteAsync(reservations);

        foreach ((string orderId, int quantity) in new[] { ("order-1001", 2), ("order-1001", 2), ("order-1002", 2), ("order-1003", 1) })
        {
            RedisResult result = await db.ExecuteAsync("FCALL", "reserve_stock", 2, StockKey, reservations, orderId, quantity);
            Console.WriteLine($"FCALL reserve_stock {orderId} x{quantity} -> {result}");
        }
        Console.WriteLine($"stock {(long)await db.StringGetAsync(StockKey)}, reservations: " +
                          string.Join(", ", (await db.HashGetAllAsync(reservations)).Select(e => $"{e.Name}={e.Value}")));
    }
}
