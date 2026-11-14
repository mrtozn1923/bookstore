using Bookstore.OsLab;

string command = args.Length > 0 ? args[0] : "help";

switch (command)
{
    case "read-catalog":
        string json = File.ReadAllText("data/catalog.json");
        Console.WriteLine($"catalog.json: {json.Length} chars");
        break;

    case "log-naive":
        OrderLog.WriteNaive("orders.log", int.Parse(args[1]));
        break;

    case "log-buffered":
        OrderLog.WriteBuffered("orders.log", int.Parse(args[1]));
        break;

    default:
        Console.WriteLine("usage: read-catalog | log-naive N | log-buffered N");
        break;
}
