using System.Diagnostics;
using Microsoft.Data.SqlClient;

namespace Bookstore.ScaleLab;

public static class PoolCommands
{
    // simulates an endpoint that forgets to dispose its connection
    public static async Task<int> LeakAsync(LabOptions options)
    {
        bool leak = options.Get("leak", "true") == "true";
        SqlConnectionStringBuilder builder = new(Db.Env("BOOKSTORE_SQL"))
        {
            MaxPoolSize = 5,
            ConnectTimeout = 3,
            ApplicationName = "Bookstore.Api"
        };
        string connectionString = builder.ConnectionString;

        for (int request = 1; request <= 7; request++)
        {
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                if (leak)
                {
                    SqlConnection connection = new(connectionString);
                    await connection.OpenAsync();
                    await new SqlCommand("SELECT COUNT(*) FROM dbo.Orders WHERE Status = 'Pending'", connection).ExecuteScalarAsync();
                }
                else
                {
                    await using SqlConnection connection = new(connectionString);
                    await connection.OpenAsync();
                    await using SqlCommand command = new("SELECT COUNT(*) FROM dbo.Orders WHERE Status = 'Pending'", connection);
                    await command.ExecuteScalarAsync();
                }
                Console.WriteLine($"request {request}: ok in {sw.ElapsedMilliseconds} ms");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"request {request}: {ex.GetType().Name} after {sw.ElapsedMilliseconds} ms");
                Console.WriteLine($"  {ex.Message}");
            }
        }
        await PrintSessionsAsync();
        return 0;
    }

    // what the server sees: sessions per application and status
    static async Task PrintSessionsAsync()
    {
        SqlConnectionStringBuilder builder = new(Db.Env("BOOKSTORE_SQL")) { Pooling = false, ApplicationName = "lab-monitor" };
        await using SqlConnection connection = new(builder.ConnectionString);
        await connection.OpenAsync();
        await using SqlCommand command = new("""
            SELECT program_name, status, open_transaction_count, COUNT(*) AS sessions
            FROM sys.dm_exec_sessions
            WHERE program_name = 'Bookstore.Api'
            GROUP BY program_name, status, open_transaction_count
            """, connection);
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            Console.WriteLine($"server: program={reader.GetString(0)} status={reader.GetString(1)} open_tran={reader.GetInt32(2)} sessions={reader.GetInt32(3)}");
        }
    }

    // opens one connection per "tenant" with a different Application Name
    public static async Task<int> FragmentAsync(LabOptions options)
    {
        int tenants = options.GetInt("tenants", 50);
        bool canonical = options.Get("canonical", "false") == "true";
        using SqlClientCounterListener listener = new();

        for (int round = 0; round < 3; round++)
        {
            await Parallel.ForAsync(1, tenants + 1, async (tenant, ct) =>
            {
                SqlConnectionStringBuilder builder = new(Db.Env("BOOKSTORE_SQL"))
                {
                    ApplicationName = canonical ? "Bookstore.Api" : $"Bookstore.Api-tenant-{tenant}"
                };
                await using SqlConnection connection = new(builder.ConnectionString);
                await connection.OpenAsync(ct);
                await using SqlCommand command = new("SELECT 1", connection);
                await command.ExecuteScalarAsync(ct);
            });
        }

        await Task.Delay(TimeSpan.FromSeconds(2.5));
        foreach (string name in new[] { "number-of-active-connection-pool-groups", "number-of-active-connection-pools",
                                         "number-of-pooled-connections", "number-of-free-connections" })
        {
            Console.WriteLine($"{name,-40} {listener.Latest.GetValueOrDefault(name)}");
        }
        return 0;
    }
}
