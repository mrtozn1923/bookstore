using System.Globalization;
using Bookstore.IO.LargeFiles;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

string command = args.Length > 0 ? args[0] : "help";

switch (command)
{
    case "generate": LargeFileDemos.Generate(args.Length > 1 ? double.Parse(args[1]) : 5); break;
    case "revenue": await LargeFileDemos.RevenueAsync(args[1], args.Length > 2 ? int.Parse(args[2]) : Environment.ProcessorCount); break;
    case "random": LargeFileDemos.Random(args[1], args.Length > 2 ? int.Parse(args[2]) : 1_000_000); break;
    case "loh": LargeFileDemos.LargeObjectHeap(); break;
    default:
        Console.WriteLine("Commands: generate [GB] | revenue <streamreader|chunked|parallel|mmap> [workers] | random <randomaccess|mmap> [count] | loh");
        break;
}
