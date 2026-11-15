namespace Bookstore.Analytics.Reports;

public sealed record PageWeek(string Page, int Clicks, int Impressions);

public sealed record PageChange(string Page, int PreviousClicks, int CurrentClicks)
{
    public int Delta => CurrentClicks - PreviousClicks;
}

public sealed record WeeklySummary(
    int PreviousSiteClicks,
    int CurrentSiteClicks,
    IReadOnlyList<PageChange> Gainers,
    IReadOnlyList<PageChange> Losers,
    IReadOnlyList<string> Alerts);

public static class WeeklyReport
{
    public static WeeklySummary Build(
        int previousSiteClicks, int currentSiteClicks,
        IEnumerable<PageWeek> previous, IEnumerable<PageWeek> current,
        int top = 3, double dropThreshold = 0.30, int minClicks = 50)
    {
        Dictionary<string, int> before = previous.ToDictionary(p => p.Page, p => p.Clicks);
        Dictionary<string, int> after = current.ToDictionary(p => p.Page, p => p.Clicks);

        List<PageChange> changes = before.Keys.Union(after.Keys)
            .Select(page => new PageChange(page, before.GetValueOrDefault(page), after.GetValueOrDefault(page)))
            .ToList();

        List<string> alerts = [];
        if (previousSiteClicks >= minClicks && currentSiteClicks < previousSiteClicks * (1 - dropThreshold))
        {
            alerts.Add($"site clicks fell {Drop(previousSiteClicks, currentSiteClicks)} week over week");
        }

        alerts.AddRange(changes
            .Where(c => c.PreviousClicks >= minClicks && c.CurrentClicks < c.PreviousClicks * (1 - dropThreshold))
            .OrderByDescending(c => c.PreviousClicks - c.CurrentClicks)
            .Select(c => $"{c.Page} fell {Drop(c.PreviousClicks, c.CurrentClicks)} ({c.PreviousClicks} -> {c.CurrentClicks})"));

        return new WeeklySummary(
            previousSiteClicks,
            currentSiteClicks,
            changes.Where(c => c.Delta > 0).OrderByDescending(c => c.Delta).ThenBy(c => c.Page, StringComparer.Ordinal).Take(top).ToList(),
            changes.Where(c => c.Delta < 0).OrderBy(c => c.Delta).ThenBy(c => c.Page, StringComparer.Ordinal).Take(top).ToList(),
            alerts);
    }

    private static string Drop(int before, int after) => $"{(before - after) * 100.0 / before:0}%";
}
