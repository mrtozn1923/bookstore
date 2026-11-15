using Bookstore.Analytics.Archive;
using Bookstore.Analytics.Reports;

namespace Bookstore.Analytics.Cli;

public static class ReportCommand
{
    public static async Task<int> RunAsync(DateOnly weekEnding, string connectionString)
    {
        SqlServerArchive archive = new(connectionString);
        DateOnly currentFrom = weekEnding.AddDays(-6);
        DateOnly previousTo = currentFrom.AddDays(-1);
        DateOnly previousFrom = previousTo.AddDays(-6);

        Task<List<PageWeek>> previous = archive.PageTotalsAsync(previousFrom, previousTo);
        Task<List<PageWeek>> current = archive.PageTotalsAsync(currentFrom, weekEnding);
        Task<int> previousSite = archive.SiteClicksAsync(previousFrom, previousTo);
        Task<int> currentSite = archive.SiteClicksAsync(currentFrom, weekEnding);
        await Task.WhenAll(previous, current, previousSite, currentSite);

        WeeklySummary s = WeeklyReport.Build(previousSite.Result, currentSite.Result, previous.Result, current.Result);

        Console.WriteLine($"week {currentFrom:yyyy-MM-dd}..{weekEnding:yyyy-MM-dd} vs {previousFrom:yyyy-MM-dd}..{previousTo:yyyy-MM-dd}");
        Console.WriteLine($"site clicks: {s.PreviousSiteClicks} -> {s.CurrentSiteClicks}");
        Print("gainers", s.Gainers);
        Print("losers", s.Losers);
        Console.WriteLine("alerts:");
        foreach (string alert in s.Alerts)
        {
            Console.WriteLine($"  ! {alert}");
        }

        return s.Alerts.Count == 0 ? 0 : 3;
    }

    private static void Print(string title, IReadOnlyList<PageChange> changes)
    {
        Console.WriteLine($"{title}:");
        foreach (PageChange c in changes)
        {
            Console.WriteLine($"  {c.Delta,+5:+0;-0}  {c.Page} ({c.PreviousClicks} -> {c.CurrentClicks})");
        }
    }
}
