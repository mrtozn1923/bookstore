using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;

namespace Bookstore.ScaleLab;

public static class LoadCommand
{
    public static async Task<int> RunAsync(LabOptions options)
    {
        string db = options.Get("db", "pg");
        int concurrency = options.GetInt("concurrency", 1);
        TimeSpan duration = TimeSpan.FromSeconds(options.GetInt("seconds", 20));
        string sql = Db.OrderHistorySql(db);

        await WarmUpAsync(db, sql);

        using CancellationTokenSource stop = new(duration);
        ConcurrentBag<double> latenciesMs = [];
        int errors = 0;
        Task<Dictionary<string, int>> sampler = SampleWaitsAsync(db, stop.Token);

        Stopwatch total = Stopwatch.StartNew();
        Task[] workers = Enumerable.Range(0, concurrency).Select(async worker =>
        {
            Random random = new(worker);
            while (!stop.IsCancellationRequested)
            {
                long started = Stopwatch.GetTimestamp();
                try
                {
                    await using DbConnection connection = Db.Create(db);
                    await connection.OpenAsync();
                    await using DbCommand command = connection.CreateCommand();
                    command.CommandText = sql;
                    DbParameter p = command.CreateParameter();
                    p.ParameterName = "customerId";
                    p.Value = random.Next(1, 100_001);
                    command.Parameters.Add(p);
                    await using DbDataReader reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync()) { }
                    latenciesMs.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
                }
                catch (DbException)
                {
                    Interlocked.Increment(ref errors);
                }
            }
        }).ToArray();

        await Task.WhenAll(workers);
        total.Stop();
        Dictionary<string, int> waits = await sampler;

        double[] sorted = [.. latenciesMs.Order()];
        Console.WriteLine($"db={db} concurrency={concurrency} requests={sorted.Length} errors={errors}");
        Console.WriteLine($"throughput={sorted.Length / total.Elapsed.TotalSeconds:F1} req/s " +
                          $"p50={Percentile(sorted, 50):F1} ms p95={Percentile(sorted, 95):F1} ms p99={Percentile(sorted, 99):F1} ms");
        int samples = waits.Values.Sum();
        foreach ((string wait, int count) in waits.OrderByDescending(w => w.Value).Take(4))
        {
            Console.WriteLine($"  active sessions on {wait}: {100.0 * count / samples:F0}%");
        }
        return 0;
    }

    static double Percentile(double[] sorted, int p) =>
        sorted.Length == 0 ? 0 : sorted[Math.Min(sorted.Length - 1, (int)Math.Ceiling(p / 100.0 * sorted.Length) - 1)];

    static async Task WarmUpAsync(string db, string sql)
    {
        await using DbConnection connection = Db.Create(db);
        await connection.OpenAsync();
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = sql.Replace("@customerId", "1");
        await command.ExecuteNonQueryAsync();
    }

    // samples the server's active requests every 200 ms: what are they doing?
    static async Task<Dictionary<string, int>> SampleWaitsAsync(string db, CancellationToken token)
    {
        Dictionary<string, int> totals = [];
        await using DbConnection connection = Db.Create(db);
        await connection.OpenAsync();
        while (!token.IsCancellationRequested)
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = Db.ActiveWaitsSql(db);
            await using DbDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string wait = reader.GetString(0);
                totals[wait] = totals.GetValueOrDefault(wait) + Convert.ToInt32(reader.GetValue(1));
            }
            await Task.Delay(200, CancellationToken.None);
        }
        return totals;
    }
}
