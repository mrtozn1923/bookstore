using System.Globalization;
using Bookstore.Analytics.Cli;

// same number format on every machine
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

string? sql = Environment.GetEnvironmentVariable("BOOKSTORE_ANALYTICS_SQL");

return args switch
{
    ["logs", string file, string host] => LogsCommand.Run(file, host),
    ["opportunities", string csv] => OpportunitiesCommand.Run(csv),
    ["coverage", string csv] => CoverageCommand.Run(csv),
    ["impact", string daily, string changes, string day] =>
        ImpactCommand.Run(daily, changes, DateOnly.Parse(day, CultureInfo.InvariantCulture)),
    ["fetch", string site, .. string[] rest] when sql is not null => await FetchCommand.RunAsync(
        site,
        Day(rest, "--from") ?? FetchCommand.LatestFinalDay(DateTimeOffset.UtcNow),
        Day(rest, "--to") ?? FetchCommand.LatestFinalDay(DateTimeOffset.UtcNow),
        new FetchCommand.Options(
            sql,
            Environment.GetEnvironmentVariable("BOOKSTORE_GSC_KEY_FILE"),
            Value(rest, "--stub") is { } stub ? new Uri(stub) : null,
            int.Parse(Value(rest, "--page-size") ?? "25000", CultureInfo.InvariantCulture))),
    ["report", string weekEnding] when sql is not null =>
        await ReportCommand.RunAsync(DateOnly.Parse(weekEnding, CultureInfo.InvariantCulture), sql),
    _ => Usage(),
};

static string? Value(string[] args, string name) =>
    Array.IndexOf(args, name) is int i and >= 0 && i + 1 < args.Length ? args[i + 1] : null;

static DateOnly? Day(string[] args, string name) =>
    Value(args, name) is { } v ? DateOnly.Parse(v, CultureInfo.InvariantCulture) : null;

static int Usage()
{
    Console.Error.WriteLine("""
        usage:
          bookstore-analytics logs <access.log> <site-host>
          bookstore-analytics opportunities <pages-queries.csv>
          bookstore-analytics coverage <coverage-daily.csv>
          bookstore-analytics impact <daily-pages.csv> <changes.csv> <change-day yyyy-MM-dd>
          bookstore-analytics fetch <site-url> [--from yyyy-MM-dd] [--to yyyy-MM-dd] [--stub http://127.0.0.1:port/] [--page-size n]
          bookstore-analytics report <week-ending yyyy-MM-dd>
        fetch and report need BOOKSTORE_ANALYTICS_SQL; fetch without --stub needs BOOKSTORE_GSC_KEY_FILE.
        """);
    return 2;
}
