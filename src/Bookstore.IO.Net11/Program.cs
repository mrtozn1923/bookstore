using System.Globalization;
using Bookstore.IO.Net11;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

switch (args.Length > 0 ? args[0] : "help")
{
    case "json": await Net11Demos.JsonAsync(); break;
    case "streams": await Net11Demos.StreamsAsync(); break;
    case "compression": Net11Demos.Compression(); break;
    case "empty": Net11Demos.Empty(); break;
    case "zip": Net11Demos.Zip(); break;
    default: Console.WriteLine("Commands: json | streams | compression | empty | zip"); break;
}
