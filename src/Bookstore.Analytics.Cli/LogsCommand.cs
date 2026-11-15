using Bookstore.Analytics.Logs;

namespace Bookstore.Analytics.Cli;

public static class LogsCommand
{
    public static int Run(string file, string host)
    {
        LandingSummary summary = LandingReport.Build(File.ReadLines(file), new TrafficClassifier(host));

        Console.WriteLine($"{"source",-20}{"requests",9}");
        foreach (TrafficSource source in Enum.GetValues<TrafficSource>())
        {
            Console.WriteLine($"{source,-20}{summary.BySource.GetValueOrDefault(source),9}");
        }

        Console.WriteLine($"unparsed lines: {summary.Unparsed}");
        Console.WriteLine();
        Console.WriteLine("top landing pages from Google Search:");
        foreach ((string path, int landings) in summary.TopGoogleLandings)
        {
            Console.WriteLine($"  {landings,3}  {path}");
        }

        return 0;
    }
}
