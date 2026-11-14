using System.Globalization;
using Bookstore.IO.Csv;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

string command = args.Length > 0 ? args[0] : "help";

switch (command)
{
    case "split": CsvDemos.Split(); break;
    case "culture": CsvDemos.Culture(); break;
    case "encoding": CsvDemos.BankEncoding(); break;
    case "import": CsvDemos.BankImport(); break;
    case "export": CsvDemos.Export(args.Length > 1 ? args[1] : "excel"); break;
    default:
        Console.WriteLine("Commands: split | culture | encoding | import | export <default|excel>");
        break;
}
