using Bookstore.ML.Lab;

// Usage: dotnet run --project src/Bookstore.ML.Lab -- <command>
string command = args.Length > 0 ? args[0] : "help";
switch (command)
{
    case "rules":
        RuleBaseline.Run();
        break;
    case "explore":
        Explore.Run();
        break;
    case "quality":
        Explore.Quality();
        break;
    case "clean":
        Clean.Run();
        break;
    default:
        Console.WriteLine("Commands: rules, explore, quality, clean");
        break;
}
