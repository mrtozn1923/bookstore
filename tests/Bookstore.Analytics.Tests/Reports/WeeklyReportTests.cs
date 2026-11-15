using Bookstore.Analytics.Reports;

namespace Bookstore.Analytics.Tests.Reports;

public class WeeklyReportTests
{
    [Fact]
    public void Finds_gainers_losers_and_alerts()
    {
        PageWeek[] previous = [new("/a", 200, 4000), new("/b", 100, 2000), new("/c", 10, 300)];
        PageWeek[] current = [new("/a", 110, 3900), new("/b", 140, 2100), new("/d", 5, 80)];

        WeeklySummary s = WeeklyReport.Build(400, 300, previous, current);

        Assert.Equal(["/b", "/d"], s.Gainers.Select(g => g.Page));
        Assert.Equal(["/a", "/c"], s.Losers.Select(l => l.Page));
        // site drop is 25%: below the 30% threshold, so only /a raises an alert
        Assert.Equal(["/a fell 45% (200 -> 110)"], s.Alerts);
    }

    [Fact]
    public void Small_pages_never_alert()
    {
        WeeklySummary s = WeeklyReport.Build(40, 10, [new("/a", 40, 900)], [new("/a", 10, 800)]);
        Assert.Empty(s.Alerts);
    }
}
