using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Bookstore.ScaleLab;

public static class ResiliencyCommands
{
    // idle connection resiliency: the server drops the session while the connection is idle
    public static async Task<int> ReconnectAsync(LabOptions options)
    {
        SqlConnectionStringBuilder builder = new(Db.Env("BOOKSTORE_SQL"))
        {
            ConnectRetryCount = options.GetInt("connect-retry-count", 1),
            ConnectRetryInterval = 2,
            Pooling = false
        };
        await using SqlConnection connection = new(builder.ConnectionString);
        await connection.OpenAsync();
        Console.WriteLine($"ConnectRetryCount={builder.ConnectRetryCount} before: spid={await SpidAsync(connection)} connection={connection.ClientConnectionId}");

        await KillSessionAsync(connection.ServerProcessId);

        try
        {
            Console.WriteLine($"after:  spid={await SpidAsync(connection)} connection={connection.ClientConnectionId}");
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"SqlException {ex.Number}: {ex.Message.Split('\n')[0]}");
        }
        return 0;
    }

    // EF Core: a retrying strategy cannot retry half of a user transaction
    public static async Task<int> UserTransactionAsync(LabOptions options)
    {
        DbContextOptions<OrderStatusContext> contextOptions = new DbContextOptionsBuilder<OrderStatusContext>()
            .UseSqlServer(Db.Env("BOOKSTORE_SQL"), sql => sql.EnableRetryOnFailure())
            .Options;
        await using OrderStatusContext db = new(contextOptions);

        try
        {
            await using IDbContextTransaction tx = await db.Database.BeginTransactionAsync();
            OrderStatus order = await db.Orders.SingleAsync(o => o.OrderId == 1);
            order.Status = "Paid";
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"InvalidOperationException: {ex.Message}");
        }

        IExecutionStrategy strategy = db.Database.CreateExecutionStrategy();
        int updated = await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction tx = await db.Database.BeginTransactionAsync();
            OrderStatus order = await db.Orders.SingleAsync(o => o.OrderId == 1);
            order.Status = "Shipped";
            int rows = await db.SaveChangesAsync();
            await tx.CommitAsync();
            return rows;
        });
        Console.WriteLine($"with CreateExecutionStrategy: updated={updated}");
        return 0;
    }

    static async Task<short> SpidAsync(SqlConnection connection)
    {
        await using SqlCommand command = new("SELECT @@SPID", connection);
        return (short)(await command.ExecuteScalarAsync() ?? 0);
    }

    static async Task KillSessionAsync(int spid)
    {
        SqlConnectionStringBuilder builder = new(Db.Env("BOOKSTORE_SQL")) { Pooling = false };
        await using SqlConnection admin = new(builder.ConnectionString);
        await admin.OpenAsync();
        await using SqlCommand kill = new($"KILL {spid}", admin);
        await kill.ExecuteNonQueryAsync();
        Console.WriteLine($"KILL {spid} from another session");
    }
}

public sealed class OrderStatus
{
    public long OrderId { get; set; }
    public required string Status { get; set; }
}

public sealed class OrderStatusContext(DbContextOptions<OrderStatusContext> options) : DbContext(options)
{
    public DbSet<OrderStatus> Orders => Set<OrderStatus>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<OrderStatus>().ToTable("Orders").HasKey(o => o.OrderId);
}
