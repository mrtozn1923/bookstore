using System.Globalization;
using Bookstore.SearchLab;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

string command = args.Length > 0 ? args[0] : "help";
string[] rest = args.Skip(1).ToArray();

switch (command)
{
    case "inverted-index":
        InvertedIndexDemo.Run(rest);
        break;
    case "first-connection":
        await FirstConnectionDemo.RunAsync();
        break;
    case "create-index":
        await IndexSetup.RunAsync(rest);
        break;
    case "translators":
        await MappingDemo.TranslatorsAsync(rest);
        break;
    case "seed-sql":
        await SqlSeeder.RunAsync(rest);
        break;
    default:
        Console.WriteLine("Commands: inverted-index [query], seed-sql [syntheticCount], first-connection, create-index [name], translators [name]");
        break;
}
