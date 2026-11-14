using System.Diagnostics;
using System.Globalization;
using Npgsql;

namespace Bookstore.ScaleLab.Partitioning;

// runs once a day: creates next months' partitions, moves months past retention to the archive schema
public sealed class PartitionMaintenanceJob(NpgsqlDataSource dataSource, TimeProvider time)
{
    public async Task RunAsync(int monthsAhead, int retentionMonths, CancellationToken ct)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        DateOnly thisMonth = new(now.Year, now.Month, 1);

        int created = await ScalarAsync<int>("SELECT create_month_partitions(@from, @to)", ct,
            ("from", thisMonth), ("to", thisMonth.AddMonths(monthsAhead)));
        Console.WriteLine($"created {created} new monthly partitions up to {thisMonth.AddMonths(monthsAhead):yyyy-MM}");

        DateOnly cutoff = thisMonth.AddMonths(-retentionMonths);
        foreach (DateOnly month in await AttachedMonthsAsync(ct))
        {
            if (month < cutoff)
            {
                await ArchiveMonthAsync(month, ct);
            }
        }
    }

    async Task ArchiveMonthAsync(DateOnly month, CancellationToken ct)
    {
        string m = month.ToString("yyyy_MM", CultureInfo.InvariantCulture);
        Stopwatch sw = Stopwatch.StartNew();
        // order_items references orders: detach the child side first and drop its foreign key
        await ExecuteAsync($"ALTER TABLE order_items_p DETACH PARTITION order_items_{m} CONCURRENTLY", ct);
        await ExecuteAsync($"ALTER TABLE order_items_{m} DROP CONSTRAINT order_items_p_order_id_created_at_fkey", ct);
        await ExecuteAsync($"ALTER TABLE orders_p DETACH PARTITION orders_{m} CONCURRENTLY", ct);
        await ExecuteAsync($"ALTER TABLE orders_{m} SET SCHEMA archive", ct);
        await ExecuteAsync($"ALTER TABLE order_items_{m} SET SCHEMA archive", ct);
        long rows = await ScalarAsync<long>($"SELECT count(*) FROM archive.orders_{m}", ct);
        Console.WriteLine($"archived {month:yyyy-MM}: {rows} orders in {sw.ElapsedMilliseconds} ms");
    }

    async Task<List<DateOnly>> AttachedMonthsAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT c.relname
            FROM pg_inherits AS i
            JOIN pg_class AS c ON c.oid = i.inhrelid
            WHERE i.inhparent = 'orders_p'::regclass
            ORDER BY c.relname
            """;
        List<DateOnly> months = [];
        await using NpgsqlCommand command = dataSource.CreateCommand(sql);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            months.Add(DateOnly.ParseExact(reader.GetString(0)["orders_".Length..] + "_01", "yyyy_MM_dd", CultureInfo.InvariantCulture));
        }
        return months;
    }

    async Task ExecuteAsync(string sql, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(ct);
    }

    async Task<T> ScalarAsync<T>(string sql, CancellationToken ct, params (string Name, object Value)[] parameters)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(sql);
        foreach ((string name, object value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }
        return (T)(await command.ExecuteScalarAsync(ct))!;
    }
}
