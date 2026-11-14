using System.Globalization;
using Bookstore.IO.Pipelines;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

string command = args.Length > 0 ? args[0] : "help";

switch (command)
{
    case "naive": await PipeDemos.NaiveAsync(); break;
    case "split": await PipeDemos.SplitAsync(); break;
    case "channel": await PipeDemos.ChannelAsync(); break;
    case "backpressure": await PipeDemos.BackpressureAsync(); break;
    case "json-pipe": await PipeDemos.JsonPipeAsync(); break;
    default:
        Console.WriteLine("Commands: naive | split | channel | backpressure | json-pipe");
        break;
}
