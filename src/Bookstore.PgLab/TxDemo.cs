using System.Collections.Concurrent;
using System.Globalization;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Bookstore.PgLab.Reporting;

namespace Bookstore.PgLab;

public static class TxDemo
{
    private const int CustomerId = 1234;

    // Rule: a customer may have at most one Pending order. Two requests check and insert at the same time.
    public static async Task WriteSkewAsync(NpgsqlDataSource dataSource, IsolationLevel isolation, bool retry)
    {
        await using (NpgsqlCommand cleanup = dataSource.CreateCommand(
            "DELETE FROM orders WHERE customer_id = 1234 AND order_number LIKE 'BK-T%'"))
        {
            await cleanup.ExecuteNonQueryAsync();
        }

        using Barrier bothChecked = new(2);
        Task<string>[] requests =
        [
            Task.Run(() => RunAsync("A")),
            Task.Run(() => RunAsync("B")),
        ];
        foreach (string line in await Task.WhenAll(requests))
        {
            Console.WriteLine(line);
        }

        await using NpgsqlCommand count = dataSource.CreateCommand(
            "SELECT count(*) FROM orders WHERE customer_id = 1234 AND status = 'Pending'");
        Console.WriteLine($"pending orders for customer {CustomerId}: {await count.ExecuteScalarAsync()}");

        async Task<string> RunAsync(string name)
        {
            int attempt = 0;
            while (true)
            {
                attempt++;
                try
                {
                    return await PlaceOrderAsync(name, attempt);
                }
                catch (PostgresException ex) when (retry && ex.SqlState == PostgresErrorCodes.SerializationFailure && attempt < 5)
                {
                    Console.WriteLine($"{name}: attempt {attempt} failed with {ex.SqlState}, retrying");
                    await Task.Delay(Random.Shared.Next(10, 50) * attempt);
                }
                catch (PostgresException ex)
                {
                    return $"{name}: {ex.SqlState} {ex.MessageText}";
                }
            }
        }

        async Task<string> PlaceOrderAsync(string name, int attempt)
        {
            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync();
            await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(isolation);

            await using NpgsqlCommand check = new(
                "SELECT count(*) FROM orders WHERE customer_id = @customerId AND status = 'Pending'", connection, transaction);
            check.Parameters.AddWithValue("customerId", CustomerId);
            long pending = (long)(await check.ExecuteScalarAsync())!;

            if (attempt == 1)
            {
                bothChecked.SignalAndWait(); // both requests have checked before either inserts
            }
            if (pending > 0)
            {
                await transaction.CommitAsync();
                return $"{name}: saw {pending} pending order, nothing inserted";
            }

            await using NpgsqlCommand insert = new("""
                INSERT INTO orders (order_number, customer_id, status, created_at, total_amount, currency)
                VALUES (@orderNumber, @customerId, 'Pending', now(), 0, 'TRY')
                """, connection, transaction);
            insert.Parameters.AddWithValue("orderNumber", $"BK-T{name}{attempt}");
            insert.Parameters.AddWithValue("customerId", CustomerId);
            await insert.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
            return $"{name}: saw 0 pending orders, inserted BK-T{name}{attempt}";
        }
    }

    // Outbox relay: several workers drain the same table without blocking each other
    public static async Task DrainQueueAsync(NpgsqlDataSource dataSource, int workers, bool skipLocked)
    {
        ConcurrentDictionary<long, string> seen = new();
        int duplicates = 0;

        async Task<int> WorkerAsync(string name)
        {
            int processed = 0;
            while (true)
            {
                await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync();
                await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
                await using NpgsqlCommand claim = new($"""
                    SELECT outbox_message_id FROM outbox_messages
                    WHERE processed_at IS NULL
                    ORDER BY outbox_message_id
                    LIMIT 10
                    FOR UPDATE {(skipLocked ? "SKIP LOCKED" : "")}
                    """, connection, transaction);
                List<long> ids = [];
                await using (NpgsqlDataReader reader = await claim.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        ids.Add(reader.GetInt64(0));
                    }
                }
                if (ids.Count == 0)
                {
                    return processed;
                }

                await Task.Delay(20); // publish to the broker
                foreach (long id in ids)
                {
                    if (!seen.TryAdd(id, name))
                    {
                        Interlocked.Increment(ref duplicates);
                    }
                }

                await using NpgsqlCommand done = new(
                    "UPDATE outbox_messages SET processed_at = now() WHERE outbox_message_id = ANY(@ids)", connection, transaction);
                done.Parameters.AddWithValue("ids", ids.ToArray());
                await done.ExecuteNonQueryAsync();
                await transaction.CommitAsync();
                processed += ids.Count;
            }
        }

        DateTime start = DateTime.UtcNow;
        int[] counts = await Task.WhenAll(Enumerable.Range(1, workers).Select(i => WorkerAsync($"worker-{i}")));
        for (int i = 0; i < counts.Length; i++)
        {
            Console.WriteLine($"worker-{i + 1}: {counts[i]} messages");
        }
        Console.WriteLine($"total: {counts.Sum()}, duplicates: {duplicates}, elapsed: {(DateTime.UtcNow - start).TotalSeconds.ToString("F1", CultureInfo.InvariantCulture)} s");
    }

    // Only one instance of the nightly stock sync may run at a time
    public static async Task AdvisoryLockAsync(NpgsqlDataSource dataSource, int instances)
    {
        async Task<string> RunAsync(string name)
        {
            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync();
            await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
            await using NpgsqlCommand tryLock = new(
                "SELECT pg_try_advisory_xact_lock(hashtext('nightly-stock-sync'))", connection, transaction);
            bool acquired = (bool)(await tryLock.ExecuteScalarAsync())!;
            if (!acquired)
            {
                return $"{name}: another instance is running, skipped";
            }

            await Task.Delay(500); // the actual sync
            await transaction.CommitAsync(); // the lock is released with the transaction
            return $"{name}: ran the sync";
        }

        foreach (string line in await Task.WhenAll(Enumerable.Range(1, instances).Select(i => RunAsync($"instance-{i}"))))
        {
            Console.WriteLine(line);
        }
    }

    // EF Core: a retrying execution strategy and a user-initiated transaction
    public static async Task EfRetryAsync(string connectionString)
    {
        DbContextOptions<ReportingContext> options = new DbContextOptionsBuilder<ReportingContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3))
            .Options;
        await using ReportingContext db = new(options);

        try
        {
            await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await db.Database.ExecuteSqlAsync(
                $"UPDATE stock_items SET quantity_reserved = quantity_reserved + 1 WHERE book_id = {1}");
            Console.WriteLine("ExecuteSql inside a user transaction: ok");
            int books = await db.Books.CountAsync();
            Console.WriteLine($"query inside a user transaction: ok ({books})");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine(ex.Message);
        }

        // the whole unit of work goes into the strategy, so a retry starts the transaction again
        IExecutionStrategy strategy = db.Database.CreateExecutionStrategy();
        int attempts = 0;
        await strategy.ExecuteAsync(async () =>
        {
            attempts++;
            await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await db.Database.ExecuteSqlAsync(
                $"UPDATE stock_items SET quantity_reserved = quantity_reserved + 1 WHERE book_id = {1}");
            if (attempts == 1)
            {
                // simulate a serialization failure on the first attempt
                await db.Database.ExecuteSqlRawAsync(
                    "DO $$ BEGIN RAISE EXCEPTION 'simulated conflict' USING ERRCODE = 'serialization_failure'; END $$");
            }
            await transaction.CommitAsync();
        });
        Console.WriteLine($"committed after {attempts} attempt(s)");
    }
}
