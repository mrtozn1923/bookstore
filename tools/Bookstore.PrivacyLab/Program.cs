using Bookstore.PrivacyLab;

string command = args.FirstOrDefault() ?? "help";
switch (command)
{
    case "hash-link":
        HashLinkDemo.Run();
        break;
    default:
        Console.WriteLine("usage: dotnet run -- <hash-link>");
        break;
}
