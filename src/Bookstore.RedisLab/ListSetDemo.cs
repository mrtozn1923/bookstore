using System.Diagnostics;
using StackExchange.Redis;

namespace Bookstore.RedisLab;

public static class ListSetDemo
{
    private const string Queue = "bookstore:queue:emails";
    private const string Processing = "bookstore:queue:emails:processing";

    // A worker that crashes after taking job order-1002, once with RPOP and once with LMOVE
    public static async Task QueueCrashAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=email-worker");
        IDatabase db = mux.GetDatabase();

        foreach (bool reliable in new[] { false, true })
        {
            await db.KeyDeleteAsync([Queue, Processing]);
            await db.ListLeftPushAsync(Queue, ["order-1001", "order-1002", "order-1003"]);
            try
            {
                while (true)
                {
                    RedisValue job = reliable
                        ? await db.ListMoveAsync(Queue, Processing, ListSide.Right, ListSide.Left) // LMOVE
                        : await db.ListRightPopAsync(Queue);                                      // RPOP
                    if (job.IsNull)
                    {
                        break;
                    }
                    if (job == "order-1002")
                    {
                        throw new InvalidOperationException("SMTP server unreachable, worker crashed");
                    }
                    await SendEmailAsync(job);
                    if (reliable)
                    {
                        await db.ListRemoveAsync(Processing, job, count: 1); // ack: LREM
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"{(reliable ? "LMOVE" : "RPOP "),-5} worker: {ex.Message}");
            }
            Console.WriteLine($"      queue: [{string.Join(", ", await db.ListRangeAsync(Queue))}]  " +
                              $"processing: [{string.Join(", ", await db.ListRangeAsync(Processing))}]");
        }
    }

    private static Task SendEmailAsync(RedisValue job)
    {
        Console.WriteLine($"      sent e-mail for {job}");
        return Task.CompletedTask;
    }

    // A blocking pop on the shared connection: what happens to the other callers?
    public static async Task BlockingPopAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=bookstore-api");
        IDatabase db = mux.GetDatabase();
        await db.KeyDeleteAsync(Queue);

        Stopwatch sw = Stopwatch.StartNew();
        Task<RedisResult> blpop = db.ExecuteAsync("BLPOP", Queue, 3); // the queue is empty: waits up to 3 seconds
        await Task.Delay(100);
        TimeSpan ping = await db.PingAsync();                            // another caller, same multiplexer
        Console.WriteLine($"PING from another caller took {ping.TotalMilliseconds:N0} ms");
        RedisResult result = await blpop;
        Console.WriteLine($"BLPOP returned {(result.IsNull ? "nil" : result.ToString())} after {sw.ElapsedMilliseconds:N0} ms");
    }

    // 7 days of unique visitors: one set per day, weekly count with SUNIONCARD
    public static async Task VisitorsAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=observer,allowAdmin=true");
        IDatabase db = mux.GetDatabase();
        IServer server = mux.GetServers()[0];
        await server.FlushDatabaseAsync();

        Random random = new(42);
        RedisKey[] days = [.. Enumerable.Range(8, 7).Select(d => (RedisKey)$"bookstore:visitors:202611{d:D2}")];
        foreach (RedisKey day in days)
        {
            RedisValue[] customerIds = [.. Enumerable.Range(0, 20_000).Select(_ => (RedisValue)random.Next(1, 50_001))];
            await db.SetAddAsync(day, customerIds);
        }

        long monday = await db.SetLengthAsync(days[0]);
        RedisResult usage = await db.ExecuteAsync("MEMORY", "USAGE", days[0]);
        RedisResult encoding = await db.ExecuteAsync("OBJECT", "ENCODING", days[0]);
        Console.WriteLine($"{days[0]}: {monday:N0} unique visitors, {(long)usage / 1024.0:F0} KB, encoding={encoding}");

        Stopwatch sw = Stopwatch.StartNew();
        long exact = await db.SetCombineLengthAsync(SetOperation.Union, days);              // SUNIONCARD
        double exactMs = sw.Elapsed.TotalMilliseconds;
        sw.Restart();
        long approx = await db.SetCombineLengthAsync(SetOperation.Union, days, approximate: true); // SUNIONCARD ... APPROX
        double approxMs = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine($"7 days, SUNIONCARD        : {exact:N0} unique visitors ({exactMs:F1} ms)");
        Console.WriteLine($"7 days, SUNIONCARD APPROX : {approx:N0} unique visitors ({approxMs:F1} ms)");
    }

    // SMEMBERS on a set with one million members versus SSCAN
    public static async Task BigSetAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=bookstore-api,allowAdmin=true");
        IDatabase db = mux.GetDatabase();
        IServer server = mux.GetServers()[0];
        RedisKey key = "bookstore:newsletter:subscribers";
        await db.KeyDeleteAsync(key);
        for (int start = 1; start <= 1_000_000; start += 10_000)
        {
            await db.SetAddAsync(key, [.. Enumerable.Range(start, 10_000).Select(i => (RedisValue)$"customer:{i}")]);
        }
        await server.SlowlogResetAsync();

        Stopwatch sw = Stopwatch.StartNew();
        RedisValue[] all = await db.SetMembersAsync(key);
        Console.WriteLine($"SMEMBERS: {all.Length:N0} members in {sw.ElapsedMilliseconds:N0} ms");

        sw.Restart();
        long count = 0;
        await foreach (RedisValue _ in db.SetScanAsync(key, pageSize: 1000))
        {
            count++;
        }
        Console.WriteLine($"SSCAN:    {count:N0} members in {sw.ElapsedMilliseconds:N0} ms");

        foreach (CommandTrace trace in await server.SlowlogGetAsync(10))
        {
            Console.WriteLine($"slowlog: {trace.Arguments[0]} {trace.Duration.TotalMilliseconds:F1} ms");
        }
        await db.KeyDeleteAsync(key);
    }
}
