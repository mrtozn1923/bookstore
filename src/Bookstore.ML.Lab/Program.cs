using System.Globalization;
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
    case "tune":
        ModelSelection.Tune();
        break;
    case "automl":
        await ModelSelection.AutoMlAsync(withLeak: args.Contains("--leak"));
        break;
    case "explain":
        ModelSelection.Explain();
        break;
    case "fairness":
        ModelSelection.Fairness();
        break;
    case "train-delivery":
        Serving.Train(DateOnly.Parse(args[1], CultureInfo.InvariantCulture), args[2]);
        break;
    case "deploy":
        Serving.Deploy(args[1], atomic: args.Contains("--atomic"));
        break;
    case "drift":
        Serving.Drift(args[1]);
        break;
    case "load-test":
        await Serving.LoadTestAsync(args[1], args[2], args[3], int.Parse(args[4], CultureInfo.InvariantCulture),
            int.Parse(args[5], CultureInfo.InvariantCulture));
        break;
    default:
        Console.WriteLine("Commands: rules, explore, quality, clean, split, cv, complexity, first-model, pitfalls, features, inspect, skew, gd, compare, cancel, topics, segments, anomalies, tune, automl, explain, fairness, train-delivery, deploy, drift, load-test");
        break;
}
