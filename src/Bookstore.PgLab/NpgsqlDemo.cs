using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using NpgsqlTypes;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Bookstore.PgLab;

public enum BookFormat
{
    Paperback,
    Hardcover,
    Ebook,
    Audiobook,
}

public sealed record BookRow(int BookId, string Title, decimal Price);

public sealed record OrderEvent(int OrderId, string EventType, string Payload, DateTime OccurredAt);

public static class NpgsqlDemo
{
    // One NpgsqlDataSource for the whole application, registered in DI
    public static async Task DefaultsAsync(string connectionString)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Services.AddNpgsqlDataSource(connectionString);
        using IHost host = builder.Build();

        NpgsqlDataSource dataSource = host.Services.GetRequiredService<NpgsqlDataSource>();
        NpgsqlConnectionStringBuilder settings = new(dataSource.ConnectionString);
        Console.WriteLine($"Pooling={settings.Pooling} MinPoolSize={settings.MinPoolSize} MaxPoolSize={settings.MaxPoolSize} " +
                          $"ConnectionIdleLifetime={settings.ConnectionIdleLifetime}s MaxAutoPrepare={settings.MaxAutoPrepare}");

        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync();
        int[] ids = [1, 2, 3, 40000];
        IEnumerable<BookRow> books = await connection.QueryAsync<BookRow>(
            "SELECT book_id AS BookId, title, price FROM books WHERE book_id = ANY(@ids) ORDER BY book_id", new { ids });
        foreach (BookRow book in books)
        {
            Console.WriteLine($"{book.BookId,6} {book.Title,-22} {book.Price.ToString(CultureInfo.InvariantCulture)}");
        }
    }

    public static async Task TypesAsync(NpgsqlDataSource dataSource)
    {
        // 13:00 in Istanbul, written without a Kind: the server has to guess the time zone
        DateTime localNoKind = new(2026, 11, 14, 13, 0, 0);
        DateTime utc = new(2026, 11, 14, 10, 0, 0, DateTimeKind.Utc);

        await using (NpgsqlConnection connection = await dataSource.OpenConnectionAsync())
        {
            string stored = await connection.ExecuteScalarAsync<string>(
                "SELECT (@value::timestamptz)::text || '  (TimeZone=' || current_setting('TimeZone') || ')'",
                new { value = localNoKind }) ?? "";
            Console.WriteLine($"Dapper, Kind=Unspecified -> {stored}");
        }

        foreach (DateTime value in new[] { localNoKind, utc })
        {
            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync();
            await using NpgsqlCommand command = new("SELECT @value", connection);
            command.Parameters.Add(new NpgsqlParameter("value", NpgsqlDbType.TimestampTz) { Value = value });
            try
            {
                DateTime back = (DateTime)(await command.ExecuteScalarAsync())!;
                Console.WriteLine($"TimestampTz, Kind={value.Kind} -> {back:yyyy-MM-dd HH:mm} Kind={back.Kind}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TimestampTz, Kind={value.Kind} -> {ex.GetType().Name}: {ex.Message}");
            }
        }

        await using (NpgsqlConnection connection = await dataSource.OpenConnectionAsync())
        {
            try
            {
                DateTimeOffset istanbul = new(2026, 11, 14, 13, 0, 0, TimeSpan.FromHours(3));
                await connection.ExecuteScalarAsync<DateTime>("SELECT @istanbul::timestamptz", new { istanbul });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DateTimeOffset +03:00 -> {ex.GetType().Name}: {ex.Message}");
            }
        }

        await using NpgsqlConnection other = await dataSource.OpenConnectionAsync();
        await TypesRestAsync(other);
    }

    private static async Task TypesRestAsync(NpgsqlConnection connection)
    {
        // date is read as DateOnly in Npgsql 10
        await using (NpgsqlCommand command = new("SELECT min(created_at)::date FROM orders", connection))
        {
            object? first = await command.ExecuteScalarAsync();
            string text = first is DateOnly day ? day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : $"{first}";
            Console.WriteLine($"date -> {first?.GetType().Name} {text}");
        }

        // jsonb: a .NET string is sent as text
        string payload = """{"carrier": "Express", "parcels": 2}""";
        try
        {
            await connection.ExecuteAsync(
                "INSERT INTO order_events (order_id, event_type, payload, occurred_at) VALUES (1, 'Shipped', @payload, now())",
                new { payload });
        }
        catch (PostgresException ex)
        {
            Console.WriteLine($"jsonb from string -> {ex.SqlState}: {ex.MessageText}");
        }
        await using (NpgsqlCommand insert = new(
            "INSERT INTO order_events (order_id, event_type, payload, occurred_at) VALUES (1, 'Shipped', @payload, now())", connection))
        {
            insert.Parameters.Add(new NpgsqlParameter("payload", NpgsqlDbType.Jsonb) { Value = payload });
            Console.WriteLine($"jsonb with NpgsqlDbType.Jsonb -> {await insert.ExecuteNonQueryAsync()} row");
        }
    }

    public static async Task EnumAsync(string connectionString)
    {
        NpgsqlDataSourceBuilder builder = new(connectionString);
        builder.MapEnum<BookFormat>("book_format");
        await using NpgsqlDataSource dataSource = builder.Build();
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync();

        await connection.ExecuteAsync("DELETE FROM book_editions WHERE book_id = 1");
        await using (NpgsqlCommand insert = new(
            "INSERT INTO book_editions (book_id, format, released_on) VALUES (1, @format, @releasedOn)", connection))
        {
            insert.Parameters.AddWithValue("format", BookFormat.Audiobook);
            insert.Parameters.AddWithValue("releasedOn", new DateOnly(2026, 11, 1));
            await insert.ExecuteNonQueryAsync();
        }
        string stored = await connection.ExecuteScalarAsync<string>("SELECT format::text FROM book_editions WHERE book_id = 1") ?? "";
        Console.WriteLine($"stored as '{stored}'");

        try
        {
            await connection.ExecuteAsync(
                "INSERT INTO book_editions (book_id, format, released_on) VALUES (1, @format, @releasedOn)",
                new { format = BookFormat.Ebook, releasedOn = new DateOnly(2026, 11, 1) });
        }
        catch (PostgresException ex)
        {
            Console.WriteLine($"Dapper with a C# enum -> {ex.SqlState}: {ex.MessageText}");
        }

        // Dapper sends enums as integers: send the label as text and cast it on the server
        await connection.ExecuteAsync(
            "INSERT INTO book_editions (book_id, format, released_on) VALUES (1, @format::book_format, @releasedOn)",
            new { format = BookFormat.Ebook.ToString().ToLowerInvariant(), releasedOn = new DateOnly(2026, 11, 1) });
        IEnumerable<BookFormat> formats = await connection.QueryAsync<BookFormat>(
            "SELECT format FROM book_editions WHERE book_id = 1 ORDER BY format");
        Console.WriteLine($"label as text + cast -> {string.Join(", ", formats)}");
    }

    public static async Task CopyAsync(NpgsqlDataSource dataSource, int rows)
    {
        List<OrderEvent> events = Enumerable.Range(1, rows)
            .Select(i => new OrderEvent(i % 1_000_000 + 1, i % 3 == 0 ? "Shipped" : "Paid",
                $$"""{"seq": {{i}}}""", new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(i)))
            .ToList();
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync();

        // 1. one INSERT per row, all in one transaction
        await connection.ExecuteAsync("DELETE FROM order_events");
        Stopwatch watch = Stopwatch.StartNew();
        await using (NpgsqlTransaction transaction = await connection.BeginTransactionAsync())
        {
            await connection.ExecuteAsync(
                "INSERT INTO order_events (order_id, event_type, payload, occurred_at) VALUES (@OrderId, @EventType, @Payload::jsonb, @OccurredAt)",
                events, transaction);
            await transaction.CommitAsync();
        }
        Console.WriteLine($"Dapper Execute(list): {watch.ElapsedMilliseconds} ms");

        // 2. NpgsqlBatch: 1000 statements per round trip
        await connection.ExecuteAsync("DELETE FROM order_events");
        watch.Restart();
        await using (NpgsqlTransaction transaction = await connection.BeginTransactionAsync())
        {
            foreach (OrderEvent[] chunk in events.Chunk(1000))
            {
                await using NpgsqlBatch batch = new(connection, transaction);
                foreach (OrderEvent e in chunk)
                {
                    NpgsqlBatchCommand command = new(
                        "INSERT INTO order_events (order_id, event_type, payload, occurred_at) VALUES ($1, $2, $3, $4)");
                    command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = e.OrderId });
                    command.Parameters.Add(new NpgsqlParameter<string> { TypedValue = e.EventType });
                    command.Parameters.Add(new NpgsqlParameter<string> { TypedValue = e.Payload, NpgsqlDbType = NpgsqlDbType.Jsonb });
                    command.Parameters.Add(new NpgsqlParameter<DateTime> { TypedValue = e.OccurredAt });
                    batch.BatchCommands.Add(command);
                }
                await batch.ExecuteNonQueryAsync();
            }
            await transaction.CommitAsync();
        }
        Console.WriteLine($"NpgsqlBatch (1000 per batch): {watch.ElapsedMilliseconds} ms");

        // 3. binary COPY
        await connection.ExecuteAsync("DELETE FROM order_events");
        watch.Restart();
        await using (NpgsqlBinaryImporter importer = await connection.BeginBinaryImportAsync(
            "COPY order_events (order_id, event_type, payload, occurred_at) FROM STDIN (FORMAT BINARY)"))
        {
            foreach (OrderEvent e in events)
            {
                await importer.StartRowAsync();
                await importer.WriteAsync(e.OrderId, NpgsqlDbType.Integer);
                await importer.WriteAsync(e.EventType, NpgsqlDbType.Text);
                await importer.WriteAsync(e.Payload, NpgsqlDbType.Jsonb);
                await importer.WriteAsync(e.OccurredAt, NpgsqlDbType.TimestampTz);
            }
            ulong written = await importer.CompleteAsync();
            Console.WriteLine($"binary COPY: {watch.ElapsedMilliseconds} ms, {written} rows");
        }
    }

    public static async Task PrepareAsync(string connectionString, int executions)
    {
        foreach (int maxAutoPrepare in new[] { 0, 20 })
        {
            NpgsqlConnectionStringBuilder settings = new(connectionString) { MaxAutoPrepare = maxAutoPrepare };
            await using NpgsqlDataSource dataSource = NpgsqlDataSource.Create(settings);
            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync();

            const string sql = """
                SELECT o.order_number, o.status, c.last_name
                FROM orders o JOIN customers c ON c.customer_id = o.customer_id
                WHERE o.order_id = @id
                """;
            Stopwatch watch = Stopwatch.StartNew();
            for (int i = 1; i <= executions; i++)
            {
                await connection.QuerySingleAsync(sql, new { id = i * 97 % 1_000_000 + 1 });
            }
            long prepared = await connection.ExecuteScalarAsync<long>("SELECT count(*) FROM pg_prepared_statements");
            Console.WriteLine($"Max Auto Prepare={maxAutoPrepare,-2}: {executions} queries in {watch.ElapsedMilliseconds} ms, " +
                              $"prepared statements on the server: {prepared}");
        }
    }

    // LISTEN/NOTIFY as a cache invalidation signal
    public static async Task ListenAsync(NpgsqlDataSource dataSource)
    {
        ConcurrentDictionary<int, decimal> priceCache = new();
        await using NpgsqlConnection listener = await dataSource.OpenConnectionAsync();
        listener.Notification += (sender, e) =>
        {
            int bookId = int.Parse(e.Payload, CultureInfo.InvariantCulture);
            priceCache.TryRemove(bookId, out _);
            Console.WriteLine($"notification on '{e.Channel}': book {bookId} evicted from the cache");
        };
        await using (NpgsqlCommand listen = new("LISTEN catalog_changed", listener))
        {
            await listen.ExecuteNonQueryAsync();
        }

        priceCache[1] = 32.77m;
        await using (NpgsqlConnection writer = await dataSource.OpenConnectionAsync())
        {
            await writer.ExecuteAsync("UPDATE books SET price = price + 1 WHERE book_id = 1");
            await using NpgsqlTransaction rolledBack = await writer.BeginTransactionAsync();
            await writer.ExecuteAsync("UPDATE books SET price = price + 1 WHERE book_id = 2", transaction: rolledBack);
            await rolledBack.RollbackAsync();
        }

        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(2));
        try
        {
            while (true)
            {
                await listener.WaitAsync(timeout.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        Console.WriteLine($"cache entries left: {priceCache.Count}");
    }

    public static async Task TraceAsync(string connectionString)
    {
        using TracerProvider tracing = Sdk.CreateTracerProviderBuilder()
            .AddNpgsql()
            .AddConsoleExporter()
            .Build();

        await using NpgsqlDataSource dataSource = NpgsqlDataSource.Create(connectionString);
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync();
        await connection.QueryAsync<BookRow>(
            "SELECT book_id AS BookId, title, price FROM books WHERE book_id = @id", new { id = 1 });
    }
}
