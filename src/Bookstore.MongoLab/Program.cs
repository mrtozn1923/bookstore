using System.Globalization;
using Bookstore.MongoLab;

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
    default:
        Console.WriteLine("Usage: dotnet run --project src/Bookstore.MongoLab -- <bson|objectid|ping|stock-race>");
        break;
}
