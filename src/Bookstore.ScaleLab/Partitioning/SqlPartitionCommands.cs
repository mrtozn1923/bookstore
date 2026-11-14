using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.ScaleLab.Partitioning;

public static partial class SqlPartitionCommands
{
    // the same monthly revenue query with three ways of passing the date range
    public static async Task<int> EliminationAsync(LabOptions options)
    {
        await using SqlConnection connection = new(Db.Env("BOOKSTORE_SQL"));
        connection.InfoMessage += (_, e) =>
        {
            foreach (SqlError message in e.Errors)
            {
                if (message.Message.StartsWith("Table 'OrdersP'", StringComparison.Ordinal))
                {
                    Console.WriteLine("  " + message.Message.Split(", physical reads")[0]);
                }
            }
        };
        await connection.OpenAsync();
        DateTime from = new(2026, 10, 1), to = new(2026, 11, 1);

        await RunAsync(connection, "AddWithValue(DateTime)", command =>
        {
            command.Parameters.AddWithValue("@from", from);
            command.Parameters.AddWithValue("@to", to);
        });
        await RunAsync(connection, "SqlDbType.DateTime2, Scale = 0", command =>
        {
            command.Parameters.Add(new SqlParameter("@from", SqlDbType.DateTime2) { Scale = 0, Value = from });
            command.Parameters.Add(new SqlParameter("@to", SqlDbType.DateTime2) { Scale = 0, Value = to });
        });
        await RunAsync(connection, "DateTime2 + CAST(@p AS datetime2(0)) in SQL", command =>
        {
            command.Parameters.Add(new SqlParameter("@from", SqlDbType.DateTime2) { Value = from });
            command.Parameters.Add(new SqlParameter("@to", SqlDbType.DateTime2) { Value = to });
        }, "CreatedAt >= CAST(@from AS datetime2(0)) AND CreatedAt < CAST(@to AS datetime2(0))");

        // EF Core: the same query through LINQ, with the column's type declared in the model
        DbContextOptions<PartitionedOrdersContext> efOptions = new DbContextOptionsBuilder<PartitionedOrdersContext>()
            .UseSqlServer(Db.Env("BOOKSTORE_SQL")).Options;
        await using PartitionedOrdersContext db = new(efOptions);
        decimal revenue = await db.Orders.Where(o => o.CreatedAt >= from && o.CreatedAt < to).SumAsync(o => o.TotalAmount);
        await using SqlCommand stats = new("""
            SELECT TOP (1) LEFT(t.text, CHARINDEX(N')SELECT', t.text)), qs.last_logical_reads
            FROM sys.dm_exec_query_stats AS qs
            CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) AS t
            WHERE t.text LIKE N'%FROM [[]OrdersP] AS%'
            ORDER BY qs.last_execution_time DESC
            """, connection);
        await using SqlDataReader r = await stats.ExecuteReaderAsync();
        await r.ReadAsync();
        Console.WriteLine($"EF Core LINQ (HasColumnType datetime2(0)): revenue={revenue}");
        Console.WriteLine($"  parameters: {r.GetString(0)}  logical reads: {r.GetInt64(1)}");
        return 0;
    }

    static async Task RunAsync(SqlConnection connection, string label, Action<SqlCommand> addParameters,
        string filter = "CreatedAt >= @from AND CreatedAt < @to")
    {
        Console.WriteLine(label);
        await using SqlCommand command = new($"""
            SET STATISTICS IO ON; SET STATISTICS XML ON;
            SELECT COUNT(*), SUM(TotalAmount) FROM dbo.OrdersP WHERE {filter};
            SET STATISTICS XML OFF; SET STATISTICS IO OFF;
            """, connection);
        addParameters(command);
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        do
        {
            while (await reader.ReadAsync())
            {
                if (reader.GetName(0).StartsWith("Microsoft SQL Server", StringComparison.Ordinal))
                {
                    string plan = reader.GetString(0);
                    Console.WriteLine($"  parameter types: {string.Join(", ", ParameterType().Matches(plan).Select(m => m.Groups[1].Value).Distinct())}");
                    Console.WriteLine($"  partitions accessed: {PartitionCount().Match(plan).Groups[1].Value}");
                }
            }
        } while (await reader.NextResultAsync());
    }

    [GeneratedRegex("ParameterDataType=\"([^\"]+)\"")]
    private static partial Regex ParameterType();

    [GeneratedRegex("PartitionsAccessed PartitionCount=\"(\\d+)\"")]
    private static partial Regex PartitionCount();
}

public sealed class OrderP
{
    public long OrderId { get; set; }
    public int CustomerId { get; set; }
    public string Status { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class PartitionedOrdersContext(DbContextOptions<PartitionedOrdersContext> options) : DbContext(options)
{
    public DbSet<OrderP> Orders => Set<OrderP>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<OrderP>(order =>
        {
            order.ToTable("OrdersP");
            order.HasKey(o => new { o.OrderId, o.CreatedAt });
            order.Property(o => o.TotalAmount).HasPrecision(12, 2);
            order.Property(o => o.CreatedAt).HasColumnType("datetime2(0)");
        });
}
