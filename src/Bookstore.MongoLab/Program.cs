using System.Globalization;
using Bookstore.MongoLab;
using Bookstore.MongoLab.EfCore;
using Microsoft.Extensions.DependencyInjection;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

string command = args.FirstOrDefault() ?? "help";
string connectionString = Environment.GetEnvironmentVariable("BOOKSTORE_MONGO") ?? "mongodb://localhost:27017";

switch (command)
{
    case "bson":
        BsonLab.Run();
        break;
    case "objectid":
        BsonLab.ObjectIds();
        break;
    case "ping":
        await ConnectionLab.PingAsync(connectionString);
        break;
    case "stock-race":
        await StockLab.RaceAsync(connectionString);
        break;
    case "guid-no-setup":
        await DriverLab.GuidWithoutSetupAsync(connectionString);
        break;
    case "watch":
        BookstoreMongo.RegisterSerialization();
        await AdvancedLab.WatchAsync(connectionString);
        break;
    case "ef-save" or "ef-query" or "ef-concurrency":
        await using (ServiceProvider services = EfLab.BuildServices(connectionString))
        {
            await (command switch
            {
                "ef-save" => EfLab.SaveAsync(services),
                "ef-query" => EfLab.QueryAsync(services),
                _ => EfLab.ConcurrencyAsync(services)
            });
        }
        break;
    case "mapping" or "query" or "editions" or "legacy-guid" or "aggregate" or "bulk" or "transaction" or "read-your-writes" or "failover":
        BookstoreMongo.RegisterSerialization();
        await using (ServiceProvider services = new ServiceCollection().AddBookstoreMongo(connectionString).BuildServiceProvider())
        {
            await (command switch
            {
                "mapping" => DriverLab.MappingAsync(services),
                "query" => DriverLab.QueryAsync(services),
                "editions" => DriverLab.EditionsAsync(services),
                "legacy-guid" => DriverLab.LegacyGuidAsync(services),
                "aggregate" => AdvancedLab.AggregateAsync(services),
                "bulk" => AdvancedLab.BulkAsync(services),
                "read-your-writes" => ConsistencyLab.ReadYourWritesAsync(services),
                "failover" => ReplicaSetLab.WatchWritesAsync(services, TimeSpan.FromSeconds(45)),
                _ => AdvancedLab.TransactionAsync(services)
            });
        }
        break;
    default:
        Console.WriteLine("Usage: dotnet run --project src/Bookstore.MongoLab -- <bson|objectid|ping|stock-race|guid-no-setup|mapping|query|editions|legacy-guid|aggregate|bulk|transaction|watch|ef-save|ef-query|ef-concurrency|read-your-writes|failover>");
        break;
}
