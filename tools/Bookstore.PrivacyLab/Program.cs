using Bookstore.PrivacyLab;

string command = args.FirstOrDefault() ?? "help";
switch (command)
{
    case "hash-link":
        HashLinkDemo.Run();
        break;
    case "inventory":
        InventoryCommand.Run(sql: args.Contains("--sql"));
        break;
    default:
        Console.WriteLine("usage: dotnet run -- <hash-link|inventory [--sql]>");
        break;
}
