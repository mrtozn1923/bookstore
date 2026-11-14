using System.Diagnostics;
using Npgsql;

namespace Bookstore.ScaleLab.Sharding;

public static class ShardingCommands
{
    static BucketShardResolver CreateResolver()
    {
        NpgsqlDataSource[] shards =
        [
            NpgsqlDataSource.Create(Db.Env("BOOKSTORE_SHARD0")),
            NpgsqlDataSource.Create(Db.Env("BOOKSTORE_SHARD1"))
        ];
        int[] bucketToShard = [.. Enumerable.Range(0, BucketShardResolver.BucketCount).Select(b => b < 32 ? 0 : 1)];
        return new BucketShardResolver(shards, bucketToShard);
    }

    // a customer's own data lives on one shard: one query, one server
    public static async Task<int> MyOrdersAsync(LabOptions options)
    {
        IShardResolver resolver = CreateResolver();
        foreach (int customerId in new[] { 4242, 4243, 77777 })
        {
            Stopwatch sw = Stopwatch.StartNew();
            await using NpgsqlCommand command = resolver.DataSourceFor(customerId).CreateCommand(
                "SELECT count(*) FROM (SELECT order_id FROM orders WHERE customer_id = $1 ORDER BY created_at DESC LIMIT 20) AS my_orders");
            command.Parameters.Add(new NpgsqlParameter { Value = customerId });
            long orders = (long)(await command.ExecuteScalarAsync())!;
            Console.WriteLine($"customer {customerId}: bucket {BucketShardResolver.BucketOf(customerId)}, shard {resolver.ShardFor(customerId)}, {orders} orders ({sw.Elapsed.TotalMilliseconds:F1} ms)");
        }
        return 0;
    }

    // identity columns are per server: two shards hand out the same order_id
    public static async Task<int> IdCollisionAsync(LabOptions options)
    {
        IShardResolver resolver = CreateResolver();
        foreach (int customerId in new[] { 4242, 4243 })
        {
            await using NpgsqlCommand command = resolver.DataSourceFor(customerId).CreateCommand(
                "INSERT INTO orders (customer_id, status, total_amount, created_at) VALUES ($1, 'Pending', 99.90, now()) RETURNING order_id");
            command.Parameters.Add(new NpgsqlParameter { Value = customerId });
            Console.WriteLine($"customer {customerId} on shard {resolver.ShardFor(customerId)}: order_id {await command.ExecuteScalarAsync()}");
        }
        return 0;
    }

    // "best sellers of the last 30 days": a cross-shard query, gathered and merged in the application
    public static async Task<int> BestSellersAsync(LabOptions options)
    {
        IShardResolver resolver = CreateResolver();
        const string perBook = """
            SELECT oi.book_id, sum(oi.quantity)::bigint AS sold
            FROM orders AS o
            JOIN order_items AS oi ON oi.order_id = o.order_id
            WHERE o.created_at >= timestamptz '2026-11-14' - interval '30 days'
            GROUP BY oi.book_id
            """;

        Stopwatch sw = Stopwatch.StartNew();
        List<(int BookId, long Sold)>[] perShard = await Task.WhenAll(resolver.AllShards.Select(shard => QueryAsync(shard, perBook)));
        Dictionary<int, long> totals = perShard.SelectMany(rows => rows)
            .GroupBy(r => r.BookId)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Sold));
        List<(int BookId, long Sold)> exact = [.. totals
            .Select(t => (BookId: t.Key, Sold: t.Value))
            .OrderByDescending(r => r.Sold).ThenBy(r => r.BookId)
            .Take(10)];
        Console.WriteLine($"exact: {perShard.Sum(r => r.Count)} rows transferred from {perShard.Length} shards, {sw.ElapsedMilliseconds} ms");

        // the tempting shortcut: each shard's own top 10, merged
        sw.Restart();
        List<(int BookId, long Sold)>[] topPerShard = await Task.WhenAll(resolver.AllShards.Select(shard =>
            QueryAsync(shard, perBook + " ORDER BY sold DESC, oi.book_id LIMIT 10")));
        List<(int BookId, long Sold)> shortcut = [.. topPerShard.SelectMany(rows => rows)
            .GroupBy(r => r.BookId)
            .Select(g => (BookId: g.Key, Sold: g.Sum(r => r.Sold)))
            .OrderByDescending(r => r.Sold).ThenBy(r => r.BookId)
            .Take(10)];
        Console.WriteLine($"top-10 per shard: {topPerShard.Sum(r => r.Count)} rows transferred, {sw.ElapsedMilliseconds} ms");

        for (int i = 0; i < 10; i++)
        {
            Console.WriteLine($"  #{i + 1,-2} exact: book {exact[i].BookId,5} sold {exact[i].Sold,3} | shortcut: book {shortcut[i].BookId,5} sold {shortcut[i].Sold,3} (really {totals[shortcut[i].BookId]})");
        }
        return 0;
    }

    static async Task<List<(int, long)>> QueryAsync(NpgsqlDataSource shard, string sql)
    {
        List<(int, long)> rows = [];
        await using NpgsqlCommand command = shard.CreateCommand(sql);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            rows.Add((reader.GetInt32(0), reader.GetInt64(1)));
        }
        return rows;
    }
}
