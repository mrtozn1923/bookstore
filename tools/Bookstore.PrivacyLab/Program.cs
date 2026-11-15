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
    case "protect":
        await ProtectDemo.RunAsync();
        break;
    case "erase":
        await EraseDemo.RunAsync();
        break;
    default:
        Console.WriteLine("usage: dotnet run -- <hash-link|inventory [--sql]|protect|erase>");
        break;
}
