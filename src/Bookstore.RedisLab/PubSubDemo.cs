using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using StackExchange.Redis;

namespace Bookstore.RedisLab;

public static class PubSubDemo
{
    private static readonly RedisChannel PriceChanged = RedisChannel.Literal("bookstore:events:price-changed");

    // At-most-once: nobody listening, listening, and a subscription connection that drops
    public static async Task DeliveryAsync(string connectionString)
    {
        await using ConnectionMultiplexer publisher = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=pricing-admin,allowAdmin=true");
        await using ConnectionMultiplexer listener = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=bookstore-api");
        ISubscriber pub = publisher.GetSubscriber();
        Stopwatch sw = Stopwatch.StartNew();
        void Log(string text) => Console.WriteLine($"{sw.Elapsed.TotalSeconds,5:F2}s {text}");

        Log($"PUBLISH 9780441013593 -> receivers: {await pub.PublishAsync(PriceChanged, "9780441013593")}");

        await listener.GetSubscriber().SubscribeAsync(PriceChanged, (_, isbn) => Log($"listener: received {isbn}"));

        Log($"PUBLISH 9780451524935 -> receivers: {await pub.PublishAsync(PriceChanged, "9780451524935")}");
        await Task.Delay(200);

        // The server drops every pub/sub connection (as a network problem or a failover would)
        long killed = await publisher.GetServers()[0].ClientKillAsync(null, ClientType.PubSub, null, skipMe: true);
        Log($"CLIENT KILL TYPE pubsub -> {killed} connection(s)");
        Log($"PUBLISH 9780141439518 -> receivers: {await pub.PublishAsync(PriceChanged, "9780141439518")}");
        await Task.Delay(3000);
        Log($"PUBLISH 9780547928227 -> receivers: {await pub.PublishAsync(PriceChanged, "9780547928227")}");
        await Task.Delay(200);
    }

    // A handler versus a ChannelMessageQueue: which one keeps the order?
    public static async Task OrderAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=bookstore-api");
        ISubscriber sub = mux.GetSubscriber();
        RedisChannel handlerChannel = RedisChannel.Literal("demo:order:handler");
        RedisChannel queueChannel = RedisChannel.Literal("demo:order:queue");
        ConcurrentQueue<int> handlerSeen = new(), queueSeen = new();
        Random random = new(3);

        await sub.SubscribeAsync(handlerChannel, (_, m) =>
        {
            Thread.Sleep(random.Next(1, 20));                     // some work of varying length
            handlerSeen.Enqueue((int)m);
        });
        ChannelMessageQueue queue = await sub.SubscribeAsync(queueChannel);
        queue.OnMessage(async message =>
        {
            await Task.Delay(random.Next(1, 20));
            queueSeen.Enqueue((int)message.Message);
        });

        for (int i = 1; i <= 10; i++)
        {
            await sub.PublishAsync(handlerChannel, i);
            await sub.PublishAsync(queueChannel, i);
        }
        await Task.Delay(1500);
        Console.WriteLine($"handler            : {string.Join(' ', handlerSeen)}");
        Console.WriteLine($"ChannelMessageQueue: {string.Join(' ', queueSeen)}");
    }

    // A subscriber that never reads: the pubsub output buffer limit closes it
    public static async Task SlowSubscriberAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=pricing-admin,allowAdmin=true");
        IServer server = mux.GetServers()[0];
        Console.WriteLine($"client-output-buffer-limit: {(await server.ConfigGetAsync("client-output-buffer-limit"))[0].Value}");

        string[] hostPort = connectionString.Split(',')[0].Split(':');
        using TcpClient raw = new();
        await raw.ConnectAsync(hostPort[0], hostPort.Length > 1 ? int.Parse(hostPort[1]) : 6379);
        byte[] subscribe = Encoding.ASCII.GetBytes(
            "*3\r\n$6\r\nCLIENT\r\n$7\r\nSETNAME\r\n$15\r\nslow-subscriber\r\n*2\r\n$9\r\nSUBSCRIBE\r\n$30\r\nbookstore:events:price-changed\r\n");
        await raw.GetStream().WriteAsync(subscribe);          // ... and never read anything again
        await Task.Delay(200);

        ISubscriber pub = mux.GetSubscriber();
        string payload = new('x', 16 * 1024);                  // a 16 KB message
        for (int i = 1; i <= 4_000; i++)
        {
            long receivers = await pub.PublishAsync(PriceChanged, payload);
            if (i % 500 == 0 || receivers == 0)
            {
                ClientInfo? slow = (await server.ClientListAsync()).FirstOrDefault(c => c.Name == "slow-subscriber");
                Console.WriteLine($"published {i,5:N0} x 16 KB  receivers={receivers}  slow-subscriber: " +
                                  (slow is null ? "gone" : $"omem={GetField(slow.Raw, "omem")}"));
                if (receivers == 0)
                {
                    break;
                }
            }
        }
    }

    private static string GetField(string? raw, string name) =>
        raw?.Split(' ').FirstOrDefault(p => p.StartsWith(name + "="))?[(name.Length + 1)..] ?? "?";

    // sharedSubscriptionConnection=true: a large reply on a connection that also has a subscription
    public static async Task SharedSubscriptionAsync(string connectionString)
    {
        await using ConnectionMultiplexer admin = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=observer");
        RedisKey big = "bookstore:report:monthly";
        await admin.GetDatabase().StringSetAsync(big, new string('r', 40 * 1024 * 1024));  // a 40 MB report

        foreach (bool shared in new[] { false, true })
        {
            await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(
                connectionString + $",name=bookstore-api,sharedSubscriptionConnection={shared},abortConnect=false,syncTimeout=5000,asyncTimeout=5000");
            int failures = 0;
            mux.ConnectionFailed += (_, e) => Interlocked.Increment(ref failures);
            try
            {
                RedisValue value = await mux.GetDatabase().StringGetAsync(big);
                Console.WriteLine($"shared={shared,-5}  GET 40 MB: {value.Length() / 1024 / 1024} MB received, connection failures={failures}");
            }
            catch (Exception ex)
            {
                await Task.Delay(500);
                Console.WriteLine($"shared={shared,-5}  GET 40 MB: {ex.GetType().Name}: {ex.Message.Split('\n')[0][..Math.Min(110, ex.Message.Split('\n')[0].Length)]}, connection failures={failures}");
            }
        }
        await admin.GetDatabase().KeyDeleteAsync(big);
    }

    // Post 05's reliable queue, now woken up by a pub/sub signal, with polling as a safety net
    public static async Task QueueSignalAsync(string connectionString)
    {
        const string queue = "bookstore:queue:emails", processing = "bookstore:queue:emails:processing";
        RedisChannel signal = RedisChannel.Literal("bookstore:queue:emails:signal");
        await using ConnectionMultiplexer api = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=bookstore-api");
        await using ConnectionMultiplexer worker = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=email-worker");
        IDatabase apiDb = api.GetDatabase(), workerDb = worker.GetDatabase();
        await apiDb.KeyDeleteAsync([queue, processing]);
        ConcurrentDictionary<string, long> enqueuedAt = new();
        Stopwatch sw = Stopwatch.StartNew();
        using CancellationTokenSource stop = new();

        ChannelMessageQueue wakeUps = await worker.GetSubscriber().SubscribeAsync(signal);
        Task loop = Task.Run(async () =>
        {
            while (!stop.IsCancellationRequested)
            {
                RedisValue job;
                while (!(job = await workerDb.ListMoveAsync(queue, processing, ListSide.Right, ListSide.Left)).IsNull)
                {
                    Console.WriteLine($"  worker: {job} picked up after {sw.ElapsedMilliseconds - enqueuedAt[job!],5} ms");
                    await workerDb.ListRemoveAsync(processing, job, count: 1);
                }
                // Sleep until a signal arrives, but never longer than the polling interval
                using CancellationTokenSource poll = CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
                poll.CancelAfter(TimeSpan.FromSeconds(2));
                try
                {
                    await wakeUps.ReadAsync(poll.Token);
                }
                catch (OperationCanceledException)
                {
                }
            }
        });

        foreach ((string job, bool publish) in new[] { ("order-1001", true), ("order-1002", true), ("order-1003", false) })
        {
            await Task.Delay(700);
            enqueuedAt[job] = sw.ElapsedMilliseconds;
            await apiDb.ListLeftPushAsync(queue, job);
            long receivers = publish ? await apiDb.PublishAsync(signal, "1") : 0;
            Console.WriteLine($"api: LPUSH {job}" + (publish ? $", PUBLISH signal -> {receivers}" : ", signal lost"));
        }
        await Task.Delay(2500);
        stop.Cancel();
        await loop;
    }

    // Expired events arrive when Redis deletes the key, not when the TTL reaches zero
    public static async Task ExpiredEventsAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=observer,allowAdmin=true");
        IServer server = mux.GetServers()[0];
        IDatabase db = mux.GetDatabase();
        await server.FlushDatabaseAsync();
        await server.ConfigSetAsync("notify-keyspace-events", "Ex");

        ConcurrentBag<long> lateness = [];
        long deadline = 0;
        Stopwatch sw = Stopwatch.StartNew();
        await mux.GetSubscriber().SubscribeAsync(RedisChannel.KeyEvent(KeyNotificationType.Expired, 0),
            (_, _) => lateness.Add(sw.ElapsedMilliseconds - Interlocked.Read(ref deadline)));

        // 20,000 verification codes that all expire at the same second and are never read again
        await Task.WhenAll(Enumerable.Range(0, 20_000).Select(i =>
            db.StringSetAsync($"bookstore:verify:{i}", "482913", TimeSpan.FromSeconds(1))));
        Interlocked.Exchange(ref deadline, sw.ElapsedMilliseconds + 1000);
        await Task.Delay(8000);

        long[] sorted = [.. lateness.Order()];
        Console.WriteLine($"expired events: {sorted.Length:N0}; after the TTL reached zero: first {sorted[0]} ms, " +
                          $"median {sorted[sorted.Length / 2]:N0} ms, last {sorted[^1]:N0} ms");
        await server.ConfigSetAsync("notify-keyspace-events", "");
    }
}
