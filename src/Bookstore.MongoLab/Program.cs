using System.Globalization;
using Bookstore.MongoLab;
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
    case "mapping" or "query" or "editions" or "legacy-guid" or "aggregate" or "bulk" or "transaction":
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
                _ => AdvancedLab.TransactionAsync(services)
            });
        }
        break;
    default:
        Console.WriteLine("Usage: dotnet run --project src/Bookstore.MongoLab -- <bson|objectid|ping|stock-race|guid-no-setup|mapping|query|editions|legacy-guid|aggregate|bulk|transaction|watch>");
        break;
}
