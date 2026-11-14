using System.Diagnostics;
using StackExchange.Redis;

namespace Bookstore.RedisLab;

public static class SyncOverAsyncDemo
{
    public static async Task RunAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=bookstore-api,asyncTimeout=2000");
        IDatabase db = mux.GetDatabase();

        // 500 "requests" arrive at once; each one blocks a thread-pool thread on a redis call
        Stopwatch sw = Stopwatch.StartNew();
        int failed = 0;
        string? firstError = null;
        Task[] requests = [.. Enumerable.Range(0, 500).Select(_ => Task.Run(() =>
        {
            try
            {
                long views = db.StringIncrementAsync("bookstore:book:42:views").Result;
            }
            catch (AggregateException ex) when (ex.InnerException is RedisTimeoutException timeout)
            {
                Interlocked.Increment(ref failed);
                firstError ??= timeout.Message;
            }
        }))];
        await Task.WhenAll(requests);
        Console.WriteLine($"500 blocking requests: {sw.ElapsedMilliseconds:N0} ms, timeouts: {failed}");
        if (firstError is not null)
        {
            Console.WriteLine(firstError);
        }

        // The same 500 requests, awaited
        sw.Restart();
        Task[] awaited = [.. Enumerable.Range(0, 500).Select(_ => Task.Run(async () =>
        {
            long views = await db.StringIncrementAsync("bookstore:book:42:views");
        }))];
        await Task.WhenAll(awaited);
        Console.WriteLine($"500 awaited requests: {sw.ElapsedMilliseconds:N0} ms");
    }
}
