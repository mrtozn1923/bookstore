using System.Globalization;
using Bookstore.IO.Compression;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

string command = args.Length > 0 ? args[0] : "help";
int count = args.Length > 1 ? int.Parse(args[1]) : 1_000;

switch (command)
{
    case "formats": CompressionDemos.Formats(); break;
    case "options": CompressionDemos.Options(); break;
    case "zip": await CompressionDemos.ZipAsync(count); break;
    case "async-check": await CompressionDemos.AsyncCheckAsync(count); break;
    case "tar": await CompressionDemos.TarAsync(count); break;
    case "two-entries": await CompressionDemos.TwoEntriesAsync(); break;
    case "slip": await CompressionDemos.SlipAsync(); break;
    case "bomb": await CompressionDemos.BombAsync(); break;
    default:
        Console.WriteLine("Commands: formats | options | zip [count] | async-check [count] | tar [count] | two-entries | slip | bomb");
        break;
}
