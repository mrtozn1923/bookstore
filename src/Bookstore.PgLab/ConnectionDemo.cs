using System.Globalization;
using Npgsql;

namespace Bookstore.PgLab;

public static class ConnectionDemo
{
    public static async Task OpenUntilRejectedAsync(string connectionString)
    {
        // Pooling=false: every Open() is a new server process
        NpgsqlConnectionStringBuilder builder = new(connectionString) { Pooling = false };
        await using NpgsqlDataSource dataSource = NpgsqlDataSource.Create(builder);

        List<NpgsqlConnection> open = [];
        try
        {
            for (int i = 1; i <= 120; i++)
            {
                NpgsqlConnection connection = await dataSource.OpenConnectionAsync();
                open.Add(connection);
                if (i <= 3)
                {
                    Console.WriteLine($"connection {i}: server process id {connection.ProcessID}");
                }
            }
        }
        catch (PostgresException ex)
        {
            Console.WriteLine($"connection {open.Count + 1}: {ex.SqlState} {ex.MessageText}");
        }

        await using (NpgsqlCommand command = new(
            "SELECT count(*) FROM pg_stat_activity WHERE backend_type = 'client backend'", open[0]))
        {
            Console.WriteLine($"client backend processes on the server: {await command.ExecuteScalarAsync()}");
        }

        foreach (NpgsqlConnection connection in open)
        {
            await connection.DisposeAsync();
        }
    }

    public static async Task RunThroughPoolAsync(string connectionString, int maxPoolSize, int requests)
    {
        NpgsqlConnectionStringBuilder builder = new(connectionString) { MaxPoolSize = maxPoolSize };
        await using NpgsqlDataSource pooled = NpgsqlDataSource.Create(builder);
        await using NpgsqlDataSource monitor = NpgsqlDataSource.Create(
            new NpgsqlConnectionStringBuilder(connectionString) { Pooling = false, ApplicationName = "monitor" });

        int peak = 0;
        using CancellationTokenSource stop = new();
        Task watcher = Task.Run(async () =>
        {
            await using NpgsqlConnection connection = await monitor.OpenConnectionAsync();
            await using NpgsqlCommand command = new(
                "SELECT count(*) FROM pg_stat_activity WHERE backend_type = 'client backend' AND application_name <> 'monitor'",
                connection);
            while (!stop.IsCancellationRequested)
            {
                int current = Convert.ToInt32(await command.ExecuteScalarAsync());
                peak = Math.Max(peak, current);
                await Task.Delay(20);
            }
        });

        System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
        await Parallel.ForEachAsync(Enumerable.Range(1, requests), new ParallelOptions { MaxDegreeOfParallelism = requests },
            async (_, ct) =>
            {
                await using NpgsqlCommand command = pooled.CreateCommand("SELECT pg_sleep(0.1)");
                await command.ExecuteNonQueryAsync(ct);
            });
        clock.Stop();
        stop.Cancel();
        await watcher;

        Console.WriteLine($"{requests} queries, Max Pool Size={maxPoolSize}");
        Console.WriteLine($"peak server processes for the app: {peak}");
        Console.WriteLine($"elapsed: {clock.Elapsed.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture)} s");
    }
}
