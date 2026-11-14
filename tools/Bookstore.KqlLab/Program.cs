using Bookstore.KqlLab;

// A small lab for the KQL post: a local stand-in for the Application Insights ingestion endpoint, and a runner that
// loads what it captured into requests/dependencies/traces/exceptions/customMetrics (and App* workspace tables)
// of an in-process KQL engine (KustoLoco).
//   dotnet run --project tools/Bookstore.KqlLab -- capture [dataDir=telemetry] [url=http://localhost:5096]
//   dotnet run --project tools/Bookstore.KqlLab -- query <file.kql> [dataDir=telemetry]
string command = args.Length > 0 ? args[0] : "";
switch (command)
{
    case "capture":
        await Capture.RunAsync(args.Length > 1 ? args[1] : "telemetry", args.Length > 2 ? args[2] : "http://localhost:5096");
        return 0;
    case "query" when args.Length > 1:
        return await QueryRunner.RunAsync(args[1], args.Length > 2 ? args[2] : "telemetry");
    default:
        Console.Error.WriteLine("usage: capture [dataDir] [url] | query <file.kql> [dataDir]");
        return 1;
}
