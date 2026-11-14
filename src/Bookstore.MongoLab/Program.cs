using System.Globalization;
using Bookstore.MongoLab;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

string command = args.FirstOrDefault() ?? "help";

switch (command)
{
    case "bson":
        BsonLab.Run();
        break;
    case "objectid":
        BsonLab.ObjectIds();
        break;
    default:
        Console.WriteLine("Usage: dotnet run --project src/Bookstore.MongoLab -- <bson|objectid>");
        break;
}
