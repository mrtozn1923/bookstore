using System.Collections.Concurrent;
using System.Diagnostics;
using Bookstore.ScaleLab.ReadRouting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Bookstore.ScaleLab.Drill;

// the end-to-end check: checkout writes and "My orders" reads against the final topology, reported per window
public static class DrillCommand
{
    sealed record Sample(double At, bool IsWrite, bool Ok, double LatencyMs, ReadTarget Target);

    public static async Task<int> RunAsync(LabOptions options)
    {
        int seconds = options.GetInt("seconds", 60), window = options.GetInt("window", 10);
        int writers = options.GetInt("writers", 4), readers = options.GetInt("readers", 16);

        NpgsqlDataSourceBuilder builder = new(Db.Env("BOOKSTORE_PG_HA"));
        builder.ConnectionStringBuilder.HostRecheckSeconds = 1;
        builder.ConnectionStringBuilder.MaxPoolSize = writers + readers + 4;
        NpgsqlMultiHostDataSource cluster = builder.BuildMultiHost();
        NpgsqlDataSource primary = cluster.WithTargetSession(TargetSessionAttributes.Primary);
        NpgsqlDataSource preferStandby = cluster.WithTargetSession(TargetSessionAttributes.PreferStandby);
        NpgsqlDataSource standbyOnly = cluster.WithTargetSession(TargetSessionAttributes.Standby);

        DbContextOptions<CheckoutContext> checkoutOptions = new DbContextOptionsBuilder<CheckoutContext>()
            .UseNpgsql(primary, npgsql => npgsql.ExecutionStrategy(dependencies => new FailoverAwareRetryStrategy(dependencies)))
            .Options;
        ReplicaLagMonitor monitor = new(standbyOnly, TimeSpan.FromMilliseconds(200));
        ReadContextFactory readFactory = new(new ReadRouter(monitor, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1)),
            preferStandby, primary, retryOnFailure: true);

        using CancellationTokenSource stop = new(TimeSpan.FromSeconds(seconds));
        using CancellationTokenSource stopMonitor = new();
        Task monitoring = monitor.RunAsync(stopMonitor.Token);
        await Task.Delay(500);

        Stopwatch clock = Stopwatch.StartNew();
        ConcurrentBag<Sample> samples = [];
        ConcurrentBag<Guid> acknowledged = [];

        Task[] writeTasks = [.. Enumerable.Range(0, writers).Select(w => Task.Run(async () =>
        {
            Random random = new(100 + w);
            while (!stop.IsCancellationRequested)
            {
                Guid key = Guid.CreateVersion7();
                long started = Stopwatch.GetTimestamp();
                bool ok = true;
                try
                {
                    await using CheckoutContext db = new(checkoutOptions);
                    IExecutionStrategy strategy = db.Database.CreateExecutionStrategy();
                    await strategy.ExecuteAsync(async ct =>
                    {
                        await using IDbContextTransaction tx = await db.Database.BeginTransactionAsync(ct);
                        await db.PlaceOrderAsync(key, random.Next(1, 100_001), 149.90m, ct);
                        await tx.CommitAsync(ct);
                    }, CancellationToken.None);
                    acknowledged.Add(key);
                }
                catch (Exception)
                {
                    ok = false;
                }
                samples.Add(new Sample(clock.Elapsed.TotalSeconds, true, ok, Stopwatch.GetElapsedTime(started).TotalMilliseconds, ReadTarget.Primary));
                await Task.Delay(50);
            }
        }))];

        Task[] readTasks = [.. Enumerable.Range(0, readers).Select(r => Task.Run(async () =>
        {
            Random random = new(200 + r);
            while (!stop.IsCancellationRequested)
            {
                int customerId = random.Next(1, 100_001);
                long started = Stopwatch.GetTimestamp();
                bool ok = true;
                ReadTarget target = ReadTarget.Replica;
                try
                {
                    await using BookstoreReadContext read = readFactory.Create(null, out target);
                    await read.Orders
                        .Where(o => o.CustomerId == customerId)
                        .OrderByDescending(o => o.CreatedAt)
                        .Take(20)
                        .ToListAsync();
                }
                catch (Exception)
                {
                    ok = false;
                }
                samples.Add(new Sample(clock.Elapsed.TotalSeconds, false, ok, Stopwatch.GetElapsedTime(started).TotalMilliseconds, target));
                await Task.Delay(10);
            }
        }))];

        await Task.WhenAll([.. writeTasks, .. readTasks]);
        stopMonitor.Cancel();
        await monitoring;

        Console.WriteLine("window   | writes ok/err | reads ok/err | replica% | read p99 ms | write p99 ms");
        foreach (IGrouping<int, Sample> w in samples.GroupBy(s => (int)(s.At / window)).OrderBy(g => g.Key))
        {
            Sample[] writes = [.. w.Where(s => s.IsWrite)], reads = [.. w.Where(s => !s.IsWrite)];
            Console.WriteLine($"{w.Key * window,3}-{(w.Key + 1) * window,3}s | {writes.Count(s => s.Ok),6}/{writes.Count(s => !s.Ok),-5} | {reads.Count(s => s.Ok),6}/{reads.Count(s => !s.Ok),-5} | " +
                              $"{100.0 * reads.Count(s => s.Target == ReadTarget.Replica) / Math.Max(1, reads.Length),7:F0}% | {P99(reads),11:F1} | {P99(writes),12:F1}");
        }
        double[] okWrites = [.. samples.Where(s => s.IsWrite && s.Ok).Select(s => s.At).Order()];
        double longestGap = okWrites.Zip(okWrites.Skip(1), (a, b) => b - a).DefaultIfEmpty(0).Max();
        Sample[] allReads = [.. samples.Where(s => !s.IsWrite)];
        Console.WriteLine($"total: writes ok={samples.Count(s => s.IsWrite && s.Ok)} failed={samples.Count(s => s.IsWrite && !s.Ok)}, " +
                          $"reads ok={allReads.Count(s => s.Ok)} failed={allReads.Count(s => !s.Ok)}, read p99={P99(allReads):F1} ms");
        Console.WriteLine($"longest gap between committed writes: {longestGap:F1} s");

        await using NpgsqlCommand count = primary.CreateCommand("SELECT count(*) FROM orders WHERE idempotency_key = ANY($1)");
        count.Parameters.Add(new NpgsqlParameter { Value = acknowledged.ToArray() });
        long found = (long)(await count.ExecuteScalarAsync())!;
        Console.WriteLine($"acknowledged orders: {acknowledged.Count}, found on the current primary: {found}, lost: {acknowledged.Count - found}");
        return 0;
    }

    static double P99(Sample[] samples)
    {
        double[] sorted = [.. samples.Where(s => s.Ok).Select(s => s.LatencyMs).Order()];
        return sorted.Length == 0 ? 0 : sorted[Math.Min(sorted.Length - 1, (int)Math.Ceiling(0.99 * sorted.Length) - 1)];
    }
}
