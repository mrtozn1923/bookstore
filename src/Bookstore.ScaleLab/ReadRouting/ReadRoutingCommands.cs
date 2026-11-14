using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NpgsqlTypes;

namespace Bookstore.ScaleLab.ReadRouting;

public static class ReadRoutingCommands
{
    public static ServiceProvider BuildServices(LabOptions options)
    {
        NpgsqlMultiHostDataSource cluster = new NpgsqlDataSourceBuilder(Db.Env("BOOKSTORE_PG_HA")).BuildMultiHost();
        NpgsqlDataSource primary = cluster.WithTargetSession(TargetSessionAttributes.Primary);
        NpgsqlDataSource preferStandby = cluster.WithTargetSession(TargetSessionAttributes.PreferStandby);
        NpgsqlDataSource standbyOnly = cluster.WithTargetSession(TargetSessionAttributes.Standby);

        ServiceCollection services = new();
        services.AddDbContext<BookstoreContext>(o => o.UseNpgsql(primary));
        bool retry = options.Get("retry", "off") == "on";
        services.AddDbContext<BookstoreReadContext>(o => o
            .UseNpgsql(preferStandby, npgsql =>
            {
                if (retry)
                {
                    // a broken pooled connection is retried on a new one; prefer-standby then falls back to the primary
                    npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(1), errorCodesToAdd: null);
                }
            })
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
        services.AddSingleton(new ReplicaLagMonitor(standbyOnly, TimeSpan.FromMilliseconds(200)));
        services.AddSingleton(sp => new ReadRouter(
            sp.GetRequiredService<ReplicaLagMonitor>(),
            maxLag: TimeSpan.FromSeconds(options.GetInt("max-lag-seconds", 2)),
            maxSampleAge: TimeSpan.FromSeconds(1)));
        services.AddSingleton(sp => new ReadContextFactory(sp.GetRequiredService<ReadRouter>(), preferStandby, primary));
        return services.BuildServiceProvider();
    }

    // which server does each context land on?
    public static async Task<int> RoutingAsync(LabOptions options)
    {
        await using ServiceProvider provider = BuildServices(options);
        for (int i = 0; i < options.GetInt("rounds", 3); i++)
        {
            using IServiceScope scope = provider.CreateScope();
            BookstoreContext write = scope.ServiceProvider.GetRequiredService<BookstoreContext>();
            BookstoreReadContext read = scope.ServiceProvider.GetRequiredService<BookstoreReadContext>();
            Stopwatch sw = Stopwatch.StartNew();
            string readServer = "error";
            try
            {
                readServer = await ServerAsync(read);
            }
            catch (Exception ex) when (ex is NpgsqlException or InvalidOperationException)
            {
                readServer = $"{ex.GetType().Name}: {ex.Message.Split('\n')[0]}";
            }
            Console.WriteLine($"write -> {await ServerAsync(write)}, read -> {readServer} ({sw.ElapsedMilliseconds} ms)");
            await Task.Delay(options.GetInt("delay-ms", 0));
        }
        return 0;
    }

    // place an order, then immediately list the customer's orders: is the new order there?
    public static async Task<int> ReadYourWritesAsync(LabOptions options)
    {
        await using ServiceProvider provider = BuildServices(options);
        ReplicaLagMonitor monitor = provider.GetRequiredService<ReplicaLagMonitor>();
        ReadContextFactory readFactory = provider.GetRequiredService<ReadContextFactory>();
        using CancellationTokenSource stop = new();
        Task monitoring = monitor.RunAsync(stop.Token);
        await Task.Delay(500);

        bool routed = options.Get("strategy", "naive") == "routed";
        int n = options.GetInt("orders", 500), missing = 0, replicaReads = 0, primaryReads = 0;
        Random random = new(6);
        Dictionary<int, NpgsqlLogSequenceNumber> lastWriteByCustomer = [];

        for (int i = 0; i < n; i++)
        {
            int customerId = random.Next(1, 100_001);
            long orderId;
            using (IServiceScope scope = provider.CreateScope())
            {
                BookstoreContext write = scope.ServiceProvider.GetRequiredService<BookstoreContext>();
                Order order = new() { CustomerId = customerId, TotalAmount = 249.90m, CreatedAt = DateTime.UtcNow };
                write.Orders.Add(order);
                await write.SaveChangesAsync();
                orderId = order.OrderId;
                lastWriteByCustomer[customerId] = await write.Database
                    .SqlQueryRaw<NpgsqlLogSequenceNumber>("SELECT pg_current_wal_lsn() AS \"Value\"")
                    .SingleAsync();
            }

            // the customer who just ordered opens "My orders", then a few other customers browse theirs
            int[] readers = [customerId, .. Enumerable.Range(0, 4).Select(_ => random.Next(1, 100_001))];
            foreach (int reader in readers)
            {
                using IServiceScope scope = provider.CreateScope();
                ReadTarget target = ReadTarget.Replica;
                BookstoreReadContext read = routed
                    ? readFactory.Create(lastWriteByCustomer.TryGetValue(reader, out NpgsqlLogSequenceNumber lsn) ? lsn : null, out target)
                    : scope.ServiceProvider.GetRequiredService<BookstoreReadContext>();
                List<long> myOrders = await read.Orders
                    .Where(o => o.CustomerId == reader)
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => o.OrderId)
                    .Take(20)
                    .ToListAsync();
                if (routed)
                {
                    await read.DisposeAsync();
                }
                if (reader == customerId && !myOrders.Contains(orderId))
                {
                    missing++;
                }
                _ = target == ReadTarget.Replica ? replicaReads++ : primaryReads++;
            }
            await Task.Delay(options.GetInt("delay-ms", 0));
        }

        ReplicaState last = monitor.Current;
        Console.WriteLine($"strategy={(routed ? "routed" : "naive")} orders={n} missing_in_list={missing} replica_reads={replicaReads} primary_reads={primaryReads}");
        Console.WriteLine($"replica: reachable={last.Reachable} lag={(last.Reachable ? last.Lag.TotalSeconds.ToString("F1") + " s" : "n/a")}");
        stop.Cancel();
        await monitoring;
        return 0;
    }

    // a "read" that writes: a function with a side effect, called through the read context
    public static async Task<int> ReadOnlyFunctionAsync(LabOptions options)
    {
        await using ServiceProvider provider = BuildServices(options);
        foreach (string target in new[] { "read", "write" })
        {
            using IServiceScope scope = provider.CreateScope();
            DbContext db = target == "read"
                ? scope.ServiceProvider.GetRequiredService<BookstoreReadContext>()
                : scope.ServiceProvider.GetRequiredService<BookstoreContext>();
            try
            {
                BookDetails book = await db.Database
                    .SqlQuery<BookDetails>($"SELECT title AS \"Title\", price AS \"Price\" FROM book_details({42})")
                    .SingleAsync();
                Console.WriteLine($"{target} context on {await ServerAsync(db)}: {book.Title} {book.Price}");
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"{target} context on {await ServerAsync(db)}: PostgresException {ex.SqlState}: {ex.MessageText}");
            }
        }
        return 0;
    }

    // SQL Server: ApplicationIntent is a request; only an availability group listener acts on it
    public static async Task<int> SqlIntentAsync(LabOptions options)
    {
        foreach (ApplicationIntent intent in new[] { ApplicationIntent.ReadWrite, ApplicationIntent.ReadOnly })
        {
            SqlConnectionStringBuilder builder = new(Db.Env("BOOKSTORE_SQL")) { ApplicationIntent = intent };
            await using SqlConnection connection = new(builder.ConnectionString);
            await connection.OpenAsync();
            await using SqlCommand command = new("""
                INSERT INTO dbo.PageViews (Page) VALUES ('my-orders');
                SELECT CONVERT(varchar(20), DATABASEPROPERTYEX(DB_NAME(), 'Updateability'));
                """, connection);
            Console.WriteLine($"ApplicationIntent={intent}: updateability={await command.ExecuteScalarAsync()}, insert ok");
        }
        return 0;
    }

    static Task<string> ServerAsync(DbContext db) =>
        db.Database.SqlQueryRaw<string>("SELECT current_setting('cluster_name') AS \"Value\"").SingleAsync();
}

public sealed record BookDetails(string Title, decimal Price);
