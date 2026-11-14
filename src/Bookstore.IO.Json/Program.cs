using System.Globalization;
using Bookstore.IO.Json;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

string command = args.Length > 0 ? args[0] : "help";

switch (command)
{
    case "defaults": JsonDemos.Defaults(); break;
    case "case": JsonDemos.CaseAndNumbers(); break;
    case "model": JsonDemos.ModelRules(); break;
    case "duplicates": JsonDemos.Duplicates(); break;
    case "strict": JsonDemos.Strict(); break;
    case "poly": JsonDemos.Polymorphism(); break;
    case "converters": JsonDemos.Converters(); break;
    case "modifiers": JsonDemos.Modifiers(); break;
    case "populate": JsonDemos.Populate(); break;
    default:
        Console.WriteLine("Commands: defaults | case | model | duplicates | strict | poly | converters | modifiers | populate");
        break;
}
