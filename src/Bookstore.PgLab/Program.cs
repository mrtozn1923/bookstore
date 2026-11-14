using Npgsql;
using Bookstore.PgLab;

string connectionString = Environment.GetEnvironmentVariable("BOOKSTORE_PG")
    ?? throw new InvalidOperationException("Set the BOOKSTORE_PG environment variable.");

switch (args.FirstOrDefault())
{
    case "connections":
        await ConnectionDemo.OpenUntilRejectedAsync(connectionString);
        break;
    case "pool":
        await ConnectionDemo.RunThroughPoolAsync(connectionString, maxPoolSize: 20, requests: 200);
        break;
    case "ef-script":
        EfDefaults.PrintCreateScript(connectionString);
        break;
    case "bestsellers":
        await Bookstore.PgLab.Reporting.Bestsellers.RunAsync(connectionString, repeat: 20);
        break;
    case "write-skew":
        await using (NpgsqlDataSource dataSource = NpgsqlDataSource.Create(connectionString))
        {
            Console.WriteLine("-- READ COMMITTED");
            await TxDemo.WriteSkewAsync(dataSource, System.Data.IsolationLevel.ReadCommitted, retry: false);
            Console.WriteLine("-- SERIALIZABLE");
            await TxDemo.WriteSkewAsync(dataSource, System.Data.IsolationLevel.Serializable, retry: false);
            Console.WriteLine("-- SERIALIZABLE with retry");
            await TxDemo.WriteSkewAsync(dataSource, System.Data.IsolationLevel.Serializable, retry: true);
        }
        break;
    case "queue":
        await using (NpgsqlDataSource dataSource = NpgsqlDataSource.Create(connectionString))
        {
            await using (NpgsqlCommand reset = dataSource.CreateCommand("UPDATE outbox_messages SET processed_at = NULL"))
            {
                await reset.ExecuteNonQueryAsync();
            }
            Console.WriteLine("-- FOR UPDATE");
            await TxDemo.DrainQueueAsync(dataSource, workers: 4, skipLocked: false);
            await using (NpgsqlCommand reset = dataSource.CreateCommand("UPDATE outbox_messages SET processed_at = NULL"))
            {
                await reset.ExecuteNonQueryAsync();
            }
            Console.WriteLine("-- FOR UPDATE SKIP LOCKED");
            await TxDemo.DrainQueueAsync(dataSource, workers: 4, skipLocked: true);
        }
        break;
    case "advisory":
        await using (NpgsqlDataSource dataSource = NpgsqlDataSource.Create(connectionString))
        {
            await TxDemo.AdvisoryLockAsync(dataSource, instances: 3);
        }
        break;
    case "ef-retry":
        await TxDemo.EfRetryAsync(connectionString);
        break;
    default:
        Console.WriteLine("usage: dotnet run -- connections | pool | ef-script | bestsellers | write-skew | queue | advisory | ef-retry");
        break;
}
