using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Bookstore.ScaleLab;

// places one order every 100 ms against a multi-host data source and reports what the app sees during a failover
public static class FailoverCommand
{
    public static async Task<int> RunAsync(LabOptions options)
    {
        TimeSpan duration = TimeSpan.FromSeconds(options.GetInt("seconds", 60));
        Stopwatch clock = Stopwatch.StartNew();
        NpgsqlDataSource dataSource = new NpgsqlDataSourceBuilder(Db.Env("BOOKSTORE_PG_HA")).BuildMultiHost()
            .WithTargetSession(TargetSessionAttributes.Primary);

        bool failoverAware = options.Get("strategy", "default") == "failover-aware";

        DbContextOptions<CheckoutContext> contextOptions = new DbContextOptionsBuilder<CheckoutContext>()
            .UseNpgsql(dataSource, npgsql =>
            {
                if (failoverAware)
                {
                    npgsql.ExecutionStrategy(dependencies => new FailoverAwareRetryStrategy(dependencies));
                }
                else
                {
                    npgsql.EnableRetryOnFailure(maxRetryCount: 10, maxRetryDelay: TimeSpan.FromSeconds(2), errorCodesToAdd: null);
                }
            })
            .LogTo((eventId, _) => eventId == CoreEventId.ExecutionStrategyRetrying,
                   eventData => Console.WriteLine($"{Stamp()} retry: {eventData.ToString().Split('\n')[0]}"))
            .Options;

        int attempts = 0, placed = 0, failed = 0;
        string? lastServer = null;
        while (clock.Elapsed < duration)
        {
            Guid key = Guid.CreateVersion7();
            attempts++;
            try
            {
                await using CheckoutContext db = new(contextOptions);
                IExecutionStrategy strategy = db.Database.CreateExecutionStrategy();
                string server = await strategy.ExecuteAsync(async ct =>
                {
                    // the whole unit of work is retried together, with the same idempotency key
                    await using IDbContextTransaction tx = await db.Database.BeginTransactionAsync(ct);
                    await db.PlaceOrderAsync(key, 4242, 99.90m, ct);
                    string host = await db.Database.SqlQueryRaw<string>("SELECT inet_server_addr()::text AS \"Value\"").SingleAsync(ct);
                    await tx.CommitAsync(ct);
                    return host;
                }, CancellationToken.None);
                placed++;
                if (server != lastServer)
                {
                    Console.WriteLine($"{Stamp()} order {placed} written on {server}");
                    lastServer = server;
                }
            }
            catch (Exception ex)
            {
                failed++;
                Console.WriteLine($"{Stamp()} order failed after retries: {ex.GetType().Name}: {ex.Message.Split('\n')[0]}");
            }
            await Task.Delay(100);
        }
        Console.WriteLine($"attempts={attempts} placed={placed} failed={failed}");
        return 0;

        string Stamp() => $"[{clock.Elapsed.TotalSeconds,5:F1}s]";
    }
}
