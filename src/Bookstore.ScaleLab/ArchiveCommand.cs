using System.Data.Common;
using System.Diagnostics;

namespace Bookstore.ScaleLab;

// moves orders older than the cutoff into the archive tables in small, resumable batches
public static class ArchiveCommand
{
    const string JobName = "orders-archive";

    public static async Task<int> RunAsync(LabOptions options)
    {
        string db = options.Get("db", "sql");
        int batchSize = options.GetInt("batch", 2000);
        TimeSpan pause = TimeSpan.FromMilliseconds(options.GetInt("pause-ms", 200));
        DateTime cutoff = DateTime.Parse(options.Get("cutoff", "2023-11-14"));

        await using DbConnection connection = Db.Create(db);
        await connection.OpenAsync();
        long lastId = await GetCheckpointAsync(connection, db);
        Console.WriteLine($"resuming after order {lastId}");

        Stopwatch total = Stopwatch.StartNew();
        long moved = 0;
        while (true)
        {
            Stopwatch sw = Stopwatch.StartNew();
            await using DbTransaction tx = await connection.BeginTransactionAsync();
            await using DbCommand command = connection.CreateCommand();
            command.Transaction = tx;
            command.CommandText = MoveBatchSql(db);
            AddParameter(command, "lastId", lastId);
            AddParameter(command, "cutoff", db == "sql" ? cutoff : DateTime.SpecifyKind(cutoff, DateTimeKind.Utc));
            AddParameter(command, "batchSize", batchSize);
            AddParameter(command, "jobName", JobName);

            await using DbDataReader reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            int count = reader.GetInt32(0);
            long maxId = reader.IsDBNull(1) ? lastId : reader.GetInt64(1);
            await reader.DisposeAsync();
            await tx.CommitAsync();

            if (count == 0)
            {
                break;
            }
            moved += count;
            lastId = maxId;
            if (moved % (batchSize * 10) == 0)
            {
                Console.WriteLine($"moved={moved} last_order_id={lastId} batch_ms={sw.ElapsedMilliseconds}");
            }
            if (count < batchSize)
            {
                break;
            }
            // gives replicas, the log and other sessions room to breathe
            await Task.Delay(pause);
        }
        Console.WriteLine($"done: moved={moved} orders in {total.Elapsed.TotalSeconds:F1} s");
        return 0;
    }

    static async Task<long> GetCheckpointAsync(DbConnection connection, string db)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = db == "sql"
            ? "SELECT LastOrderId FROM dbo.ArchiveProgress WHERE JobName = @jobName"
            : "SELECT last_order_id FROM archive_progress WHERE job_name = @jobName";
        AddParameter(command, "jobName", JobName);
        object? value = await command.ExecuteScalarAsync();
        return value is null or DBNull ? 0 : Convert.ToInt64(value);
    }

    static void AddParameter(DbCommand command, string name, object value)
    {
        DbParameter p = command.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        command.Parameters.Add(p);
    }

    static string MoveBatchSql(string db) => db == "sql"
        ? """
          SET XACT_ABORT ON;
          DECLARE @batch TABLE (OrderId bigint PRIMARY KEY);

          INSERT INTO @batch (OrderId)
          SELECT TOP (@batchSize) OrderId
          FROM dbo.Orders
          WHERE OrderId > @lastId AND CreatedAt < @cutoff
          ORDER BY OrderId;

          DELETE oi
          OUTPUT DELETED.OrderId, DELETED.LineNumber, DELETED.BookId, DELETED.Quantity, DELETED.UnitPrice
          INTO dbo.OrderItemsArchive (OrderId, LineNumber, BookId, Quantity, UnitPrice)
          FROM dbo.OrderItems AS oi
          JOIN @batch AS b ON b.OrderId = oi.OrderId;

          DELETE o
          OUTPUT DELETED.OrderId, DELETED.CustomerId, DELETED.Status, DELETED.TotalAmount, DELETED.Currency, DELETED.CreatedAt
          INTO dbo.OrdersArchive (OrderId, CustomerId, Status, TotalAmount, Currency, CreatedAt)
          FROM dbo.Orders AS o
          JOIN @batch AS b ON b.OrderId = o.OrderId;

          DECLARE @count int = (SELECT COUNT(*) FROM @batch);
          DECLARE @maxId bigint = (SELECT MAX(OrderId) FROM @batch);

          MERGE dbo.ArchiveProgress AS t
          USING (SELECT @jobName AS JobName) AS s ON t.JobName = s.JobName
          WHEN MATCHED AND @count > 0 THEN
              UPDATE SET LastOrderId = @maxId, MovedOrders = t.MovedOrders + @count, UpdatedAt = SYSUTCDATETIME()
          WHEN NOT MATCHED AND @count > 0 THEN
              INSERT (JobName, LastOrderId, MovedOrders, UpdatedAt) VALUES (@jobName, @maxId, @count, SYSUTCDATETIME());

          SELECT @count, @maxId;
          """
        : """
          WITH batch AS (
              SELECT order_id FROM orders
              WHERE order_id > @lastId AND created_at < @cutoff
              ORDER BY order_id
              LIMIT @batchSize
          ),
          moved_items AS (
              DELETE FROM order_items AS oi USING batch AS b
              WHERE oi.order_id = b.order_id
              RETURNING oi.*
          ),
          archived_items AS (
              INSERT INTO order_items_archive SELECT * FROM moved_items
          ),
          moved_orders AS (
              DELETE FROM orders AS o USING batch AS b
              WHERE o.order_id = b.order_id
              RETURNING o.*
          ),
          archived_orders AS (
              INSERT INTO orders_archive SELECT * FROM moved_orders RETURNING order_id
          ),
          progress AS (
              INSERT INTO archive_progress (job_name, last_order_id, moved_orders, updated_at)
              SELECT @jobName, max(order_id), count(*), now() FROM archived_orders HAVING count(*) > 0
              ON CONFLICT (job_name) DO UPDATE
              SET last_order_id = EXCLUDED.last_order_id,
                  moved_orders = archive_progress.moved_orders + EXCLUDED.moved_orders,
                  updated_at = EXCLUDED.updated_at
          )
          SELECT count(*)::int, max(order_id) FROM archived_orders;
          """;
}
