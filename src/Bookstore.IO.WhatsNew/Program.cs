using System.Globalization;
using Bookstore.IO.WhatsNew;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

switch (args.Length > 0 ? args[0] : "help")
{
    case "net6": await Net6.RunAsync(); break;
    case "net7": await Net7.RunAsync(); break;
    case "net8": Net8.Run(); break;
    case "net9": await Net9.RunAsync(); break;
    case "net10": await Net10.RunAsync(); break;
    case "before": await Before.RunAsync(); break;
    default: Console.WriteLine("Commands: net6 | net7 | net8 | net9 | net10 | before"); break;
}
