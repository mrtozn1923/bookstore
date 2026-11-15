using System.Data;
using Dapper;
using Npgsql;

namespace Bookstore.PgLab;

public sealed record ReservedLine(int BookId, int Quantity, int Available);

public static class Programmability
{
    // A function that returns rows: called with SELECT, like a table
    public static async Task ReserveAsync(NpgsqlDataSource dataSource, int orderId)
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync();
        try
        {
            IEnumerable<ReservedLine> lines = await connection.QueryAsync<ReservedLine>(
                "SELECT book_id, quantity, available FROM reserve_stock(@orderId)", new { orderId });
            foreach (ReservedLine line in lines)
            {
                Console.WriteLine($"order {orderId}: book {line.BookId} x{line.Quantity}, available {line.Available}");
            }
        }
        catch (PostgresException ex) when (ex.SqlState is "BK001" or "BK002")
        {
            Console.WriteLine($"order {orderId}: {ex.SqlState} {ex.MessageText}");
            Console.WriteLine($"  detail: {ex.Detail ?? "-"}, hint: {ex.Hint ?? "-"}");
        }
    }

    // A procedure: called with CALL; NOTICE messages arrive through the Notice event
    public static async Task ArchiveAsync(NpgsqlDataSource dataSource, DateTime deliveredBefore)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync();
        connection.Notice += (_, e) => Console.WriteLine($"NOTICE: {e.Notice.MessageText}");

        await using NpgsqlCommand call = new("CALL archive_shipments(@deliveredBefore, @batchSize)", connection);
        call.Parameters.AddWithValue("deliveredBefore", deliveredBefore);
        call.Parameters.AddWithValue("batchSize", 5_000);
        await call.ExecuteNonQueryAsync();

        // CommandType.StoredProcedure: Npgsql 7+ sends CALL, which is not valid for a function
        await using NpgsqlCommand asProcedure = new("reserve_stock", connection)
        {
            CommandType = CommandType.StoredProcedure,
        };
        asProcedure.Parameters.AddWithValue("p_order_id", 999360);
        try
        {
            await asProcedure.ExecuteNonQueryAsync();
        }
        catch (PostgresException ex)
        {
            Console.WriteLine($"{ex.SqlState}: {ex.MessageText} | {ex.Hint}");
        }
    }
}
