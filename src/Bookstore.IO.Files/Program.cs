using System.Globalization;
using Bookstore.IO.Files;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

string command = args.Length > 0 ? args[0] : "help";

switch (command)
{
    case "options": await FileDemos.OptionsAsync(); break;
    case "buffering": await FileDemos.BufferingAsync(); break;
    case "read-lines": await FileDemos.ReadLinesAsync(); break;
    case "random-access": await FileDemos.RandomAccessAsync(); break;
    case "atomic": await FileDemos.AtomicAsync(); break;
    case "create-new": await FileDemos.CreateNewAsync(); break;
    case "paths": FileDemos.Paths(); break;
    case "drop": await OrderFileProducer.DropAsync(args[1], args[2]); break;
    default:
        Console.WriteLine("Commands: options | buffering | read-lines | random-access | atomic | create-new | paths | drop <inbox> slow|atomic");
        break;
}
