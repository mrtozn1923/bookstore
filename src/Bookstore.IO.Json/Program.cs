using System.Globalization;
using Bookstore.IO.Json;
using Bookstore.IO.Json.Streaming;

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
    case "startup": StreamingDemos.Startup(args[1]); break;
    case "no-reflection": StreamingDemos.NoReflection(); break;
    case "export": await StreamingDemos.ExportAsync(args.Length > 1 ? int.Parse(args[1]) : 1_000_000); break;
    case "summary": await StreamingDemos.SummaryAsync(args.Length > 1 ? args[1] : "stream"); break;
    case "dom": StreamingDemos.Dom(); break;
    case "schema": StreamingDemos.Schema(); break;
    case "lenient": StreamingDemos.Lenient(); break;
    default:
        Console.WriteLine("Commands: defaults | case | model | duplicates | strict | poly | converters | modifiers | populate\n" +
                          "          startup <reflection|sourcegen> | no-reflection | export [count] | summary <list|stream|reader> | dom | schema | lenient");
        break;
}
