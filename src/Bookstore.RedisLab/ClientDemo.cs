using System.Diagnostics;
using StackExchange.Redis;

namespace Bookstore.RedisLab;

public static class ClientDemo
{
    // Anti-pattern: a new multiplexer for every "request".
    public static async Task MultiplexerPerRequestAsync(string connectionString, bool dispose)
    {
        await using ConnectionMultiplexer admin = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=observer,allowAdmin=true");
        IServer server = admin.GetServers()[0];
        long before = await GetStatAsync(server, "stats", "total_connections_received");

        List<ConnectionMultiplexer> leaked = [];
        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < 200; i++)
        {
            ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=per-request");
            await mux.GetDatabase().StringIncrementAsync("bookstore:book:42:views");
            if (dispose)
            {
                await mux.DisposeAsync();
            }
            else
            {
                leaked.Add(mux); // nobody disposes it: the connection stays open
            }
        }
        sw.Stop();

        long after = await GetStatAsync(server, "stats", "total_connections_received");
        long clients = await GetStatAsync(server, "clients", "connected_clients");
        Console.WriteLine($"200 requests: {sw.ElapsedMilliseconds:N0} ms, {sw.Elapsed.TotalMilliseconds / 200:F2} ms/request, " +
                          $"new connections: {after - before}, connected_clients now: {clients}");
    }

    public static async Task SingletonAsync(string connectionString)
    {
        await using ConnectionMultiplexer admin = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=observer,allowAdmin=true");
        IServer server = admin.GetServers()[0];
        long before = await GetStatAsync(server, "stats", "total_connections_received");

        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=bookstore-api");
        IDatabase db = mux.GetDatabase();
        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < 200; i++)
        {
            await db.StringIncrementAsync("bookstore:book:42:views");
        }
        sw.Stop();

        long after = await GetStatAsync(server, "stats", "total_connections_received");
        Console.WriteLine($"200 requests: {sw.ElapsedMilliseconds:N0} ms, {sw.Elapsed.TotalMilliseconds / 200:F2} ms/request, " +
                          $"new connections: {after - before}");
    }

    public static async Task PipelineAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=bookstore-api");
        IDatabase db = mux.GetDatabase();
        const int count = 10_000;

        // 1) await every command before sending the next one
        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < count; i++)
        {
            await db.StringIncrementAsync("bookstore:demo:sequential");
        }
        Console.WriteLine($"sequential await : {sw.ElapsedMilliseconds,6:N0} ms");

        // 2) start all commands, then await them together: the multiplexer pipelines them
        sw.Restart();
        Task<long>[] pending = new Task<long>[count];
        for (int i = 0; i < count; i++)
        {
            pending[i] = db.StringIncrementAsync("bookstore:demo:concurrent");
        }
        await Task.WhenAll(pending);
        Console.WriteLine($"Task.WhenAll     : {sw.ElapsedMilliseconds,6:N0} ms");

        // 3) IBatch: buffered on the client and written to the socket in one go on Execute()
        sw.Restart();
        IBatch batch = db.CreateBatch();
        Task<long>[] batched = new Task<long>[count];
        for (int i = 0; i < count; i++)
        {
            batched[i] = batch.StringIncrementAsync("bookstore:demo:batch");
        }
        batch.Execute();
        await Task.WhenAll(batched);
        Console.WriteLine($"IBatch           : {sw.ElapsedMilliseconds,6:N0} ms");

        // 4) fire-and-forget: no reply is awaited, the return value is always default
        sw.Restart();
        for (int i = 0; i < count; i++)
        {
            await db.StringIncrementAsync("bookstore:demo:faf", flags: CommandFlags.FireAndForget);
        }
        long faf = (long)await db.StringGetAsync("bookstore:demo:faf"); // a normal command; replies arrive in order
        Console.WriteLine($"FireAndForget    : {sw.ElapsedMilliseconds,6:N0} ms (value after a normal GET: {faf:N0})");
    }

    public static async Task SubscriptionConnectionAsync(string connectionString)
    {
        await using ConnectionMultiplexer admin = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=observer,allowAdmin=true");
        IServer server = admin.GetServers()[0];

        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=bookstore-api");
        await mux.GetDatabase().PingAsync();
        Console.WriteLine("after connect:");
        await PrintClientsAsync(server);

        await mux.GetSubscriber().SubscribeAsync(RedisChannel.Literal("bookstore:prices"), (_, _) => { });
        Console.WriteLine("after SubscribeAsync:");
        await PrintClientsAsync(server);
    }

    public static async Task ScanVersusPingAsync(string connectionString)
    {
        await using ConnectionMultiplexer instanceA = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=instance-a");
        await using ConnectionMultiplexer instanceB = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=ops-script");
        IDatabase dbA = instanceA.GetDatabase();

        List<double> pings = [];
        using CancellationTokenSource stop = new();
        Task pinger = Task.Run(async () =>
        {
            while (!stop.IsCancellationRequested)
            {
                TimeSpan rtt = await dbA.PingAsync();
                pings.Add(rtt.TotalMilliseconds);
                await Task.Delay(10);
            }
        });

        // IServer.KeysAsync uses SCAN: many small calls instead of one long one
        IServer server = instanceB.GetServers()[0];
        Stopwatch sw = Stopwatch.StartNew();
        long count = 0;
        await foreach (RedisKey _ in server.KeysAsync(pattern: "bookstore:book:*", pageSize: 1000))
        {
            count++;
        }
        Console.WriteLine($"instance B: SCAN found {count:N0} keys in {sw.ElapsedMilliseconds:N0} ms");

        stop.Cancel();
        await pinger;
        double[] sorted = [.. pings.Order()];
        Console.WriteLine($"instance A: {pings.Count} PINGs, median {sorted[sorted.Length / 2]:F2} ms, " +
                          $"p99 {sorted[(int)(sorted.Length * 0.99)]:F2} ms, max {sorted[^1]:F2} ms");
    }

    public static async Task ServerInfoAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=bookstore-api");
        IServer server = mux.GetServers()[0];
        RedisResult hello = await mux.GetDatabase().ExecuteAsync("HELLO");
        Dictionary<string, RedisResult> map = hello.ToDictionary();
        Console.WriteLine($"{server.EndPoint}: server={map["server"]}, version={map["version"]}, " +
                          $"protocol={server.Protocol}, ServerType={server.ServerType}, Version={server.Version}");
    }

    public static async Task KeyNameCostAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=observer,allowAdmin=true");
        IDatabase db = mux.GetDatabase();
        IServer server = mux.GetServers()[0];

        foreach ((string label, Func<int, string> name) in new (string, Func<int, string>)[]
        {
            ("bookstore:book:{id}:views", id => $"bookstore:book:{id}:views"),
            ("b:{id}:v", id => $"b:{id}:v"),
        })
        {
            await server.FlushDatabaseAsync();
            long before = await GetStatAsync(server, "memory", "used_memory");
            for (int start = 1; start <= 1_000_000; start += 10_000)
            {
                await Task.WhenAll(Enumerable.Range(start, 10_000).Select(id => db.StringSetAsync(name(id), id % 500)));
            }
            long after = await GetStatAsync(server, "memory", "used_memory");
            RedisValue usage = (await db.ExecuteAsync("MEMORY", "USAGE", name(42))).ToString();
            Console.WriteLine($"{label,-26} 1,000,000 keys: {(after - before) / 1024.0 / 1024.0,6:F1} MB, MEMORY USAGE of one key: {usage} bytes");
        }
        await server.FlushDatabaseAsync();
    }

    private static async Task PrintClientsAsync(IServer server)
    {
        foreach (ClientInfo client in await server.ClientListAsync())
        {
            if (client.Name != "observer")
            {
                Console.WriteLine($"  id={client.Id} name={client.Name} resp={client.Protocol} flags={client.FlagsRaw} sub={client.SubscriptionCount} cmd={client.LastCommand}");
            }
        }
    }

    private static async Task<long> GetStatAsync(IServer server, string section, string name)
    {
        IGrouping<string, KeyValuePair<string, string>>[] info = await server.InfoAsync(section);
        return long.Parse(info.SelectMany(g => g).First(kv => kv.Key == name).Value);
    }
}
