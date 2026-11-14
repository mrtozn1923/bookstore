using System.Data.Common;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Bookstore.ScaleLab;

public static class Db
{
    public static DbConnection Create(string db) => db switch
    {
        "pg" => new NpgsqlConnection(Env("BOOKSTORE_PG")),
        "pgb" => new NpgsqlConnection(Env("BOOKSTORE_PGB")),
        "sql" => new SqlConnection(Env("BOOKSTORE_SQL")),
        _ => throw new ArgumentException($"unknown db '{db}'")
    };

    public static string Env(string name) =>
        Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"{name} is not set");

    public static string OrderHistorySql(string db) => db != "sql"
        ? "SELECT order_id, status, total_amount, created_at FROM orders WHERE customer_id = @customerId ORDER BY created_at DESC LIMIT 20"
        : "SELECT TOP (20) OrderId, Status, TotalAmount, CreatedAt FROM dbo.Orders WHERE CustomerId = @customerId ORDER BY CreatedAt DESC";

    public static string ActiveWaitsSql(string db) => db != "sql"
        ? """
          SELECT coalesce(wait_event_type || ':' || wait_event, 'CPU') AS wait, count(*)
          FROM pg_stat_activity
          WHERE state = 'active' AND backend_type = 'client backend' AND pid <> pg_backend_pid()
          GROUP BY 1
          """
        : """
          SELECT coalesce(r.wait_type, 'CPU/RUNNABLE') AS wait, count(*)
          FROM sys.dm_exec_requests AS r
          WHERE r.database_id = DB_ID() AND r.session_id <> @@SPID
          GROUP BY coalesce(r.wait_type, 'CPU/RUNNABLE')
          """;
}
