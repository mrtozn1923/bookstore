using System.Globalization;
using Bookstore.IO.Streams;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

string command = args.Length > 0 ? args[0] : "help";

switch (command)
{
    case "capabilities": await Demos.CapabilitiesAsync(); break;
    case "short-read": await Demos.ShortReadAsync(); break;
    case "export": await Demos.ExportAsync(); break;
    case "truncated": Demos.Truncated(); break;
    case "leave-open": Demos.LeaveOpen(); break;
    case "counting": await Demos.CountingAsync(); break;
    case "memory": await Demos.MemoryAsync(); break;
    default:
        Console.WriteLine("Commands: capabilities | short-read | export | truncated | leave-open | counting | memory");
        break;
}
