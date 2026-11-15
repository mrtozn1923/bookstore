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
    case "defaults":
        await NpgsqlDemo.DefaultsAsync(connectionString);
        break;
    case "types":
        await using (NpgsqlDataSource dataSource = NpgsqlDataSource.Create(connectionString))
        {
            await NpgsqlDemo.TypesAsync(dataSource);
        }
        break;
    case "enum":
        await NpgsqlDemo.EnumAsync(connectionString);
        break;
    case "copy":
        await using (NpgsqlDataSource dataSource = NpgsqlDataSource.Create(connectionString))
        {
            await NpgsqlDemo.CopyAsync(dataSource, rows: 100_000);
        }
        break;
    case "prepare":
        await NpgsqlDemo.PrepareAsync(connectionString, executions: 5_000);
        break;
    case "listen":
        await using (NpgsqlDataSource dataSource = NpgsqlDataSource.Create(connectionString))
        {
            await NpgsqlDemo.ListenAsync(dataSource);
        }
        break;
    case "otel":
        await NpgsqlDemo.TraceAsync(connectionString);
        break;
    case "ef-seed":
        await Bookstore.PgLab.Catalog.CatalogDemo.SeedAsync(connectionString);
        break;
    case "ef-query":
        await Bookstore.PgLab.Catalog.CatalogDemo.QueryAsync(connectionString);
        break;
    case "ef-concurrency":
        await Bookstore.PgLab.Catalog.CatalogDemo.ConcurrencyAsync(connectionString);
        break;
    case "tls":
        await SecurityDemo.TlsAsync(connectionString, Environment.GetEnvironmentVariable("BOOKSTORE_PG_ROOT_CERT"));
        break;
    case "top-books":
        await Bookstore.PgLab.Reporting.TopBooksReport.RunAsync(connectionString);
        break;
    case "reserve":
        await using (NpgsqlDataSource dataSource = NpgsqlDataSource.Create(connectionString))
        {
            foreach (string id in args.Skip(1))
            {
                await Programmability.ReserveAsync(dataSource, int.Parse(id));
            }
        }
        break;
    case "archive":
        await using (NpgsqlDataSource dataSource = NpgsqlDataSource.Create(connectionString))
        {
            await Programmability.ArchiveAsync(dataSource, new DateTime(2023, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        }
        break;
    case "ef-trigger":
        await Bookstore.PgLab.Triggers.TriggerDemo.RunAsync(connectionString);
        break;
    default:
        Console.WriteLine("usage: dotnet run -- connections | pool | ef-script | bestsellers | write-skew | queue | advisory | ef-retry\n" +
            "       defaults | types | enum | copy | prepare | listen | otel | ef-seed | ef-query | ef-concurrency | tls | top-books\n" +
            "       reserve <order-id>... | archive | ef-trigger");
        break;
}
