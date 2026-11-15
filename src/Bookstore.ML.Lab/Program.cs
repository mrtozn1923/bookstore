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
    case "split":
        Splits.Run();
        break;
    case "cv":
        Splits.CrossValidation();
        break;
    case "complexity":
        Splits.Complexity();
        break;
    case "first-model":
        FirstModel.Run();
        break;
    case "pitfalls":
        Pitfalls.Run();
        break;
    case "features":
        FeatureLab.Run();
        break;
    case "inspect":
        FeatureLab.Inspect();
        break;
    case "skew":
        FeatureLab.Skew();
        break;
    case "gd":
        Regression.GradientDescent();
        break;
    case "compare":
        Regression.Compare();
        break;
    case "cancel":
        Classification.Cancel();
        break;
    case "topics":
        Classification.Topics();
        break;
    case "segments":
        Unsupervised.Segments();
        break;
    case "anomalies":
        Unsupervised.Anomalies();
        break;
    default:
        Console.WriteLine("Commands: rules, explore, quality, clean, split, cv, complexity, first-model, pitfalls, features, inspect, skew, gd, compare, cancel, topics, segments, anomalies");
        break;
}
