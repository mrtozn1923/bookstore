using System.Globalization;

namespace Bookstore.Analytics.Impact;

public sealed record DailyPage(DateOnly Date, string Page, int Clicks, int Impressions);

public sealed record GroupWindow(int Clicks, int Impressions)
{
    public double Ctr => Impressions == 0 ? 0 : (double)Clicks / Impressions;
}

public sealed record ImpactResult(
    GroupWindow TestBefore,
    GroupWindow TestAfter,
    GroupWindow ControlBefore,
    GroupWindow ControlAfter,
    double RelativeEffect,
    IReadOnlyList<double> BaselineWeeklyRatios,
    IReadOnlyList<double> AfterWeeklyRatios)
{
    public double RelativeCtrEffect =>
        TestAfter.Ctr / TestBefore.Ctr / (ControlAfter.Ctr / ControlBefore.Ctr) - 1;

    public double BaselineMin => BaselineWeeklyRatios.Min();
    public double BaselineMax => BaselineWeeklyRatios.Max();

    // Every week after the change outside the range seen in the weeks before it.
    public bool OutsideNormalVariation =>
        AfterWeeklyRatios.All(r => r > BaselineMax) || AfterWeeklyRatios.All(r => r < BaselineMin);
}

public static class ChangeImpactAnalyzer
{
    public static IEnumerable<DailyPage> ReadCsv(string path) =>
        File.ReadLines(path).Skip(1).Where(l => l.Length > 0).Select(l => l.Split(',')).Select(c => new DailyPage(
            DateOnly.Parse(c[0], CultureInfo.InvariantCulture), c[1],
            int.Parse(c[2], CultureInfo.InvariantCulture), int.Parse(c[3], CultureInfo.InvariantCulture)));

    // The change day itself is left out. Windows are whole weeks, so each weekday counts equally.
    public static ImpactResult Analyze(
        IReadOnlyList<DailyPage> days, IReadOnlySet<string> testPages, DateOnly changeDay,
        int windowWeeks = 4, int baselineWeeks = 12)
    {
        DateOnly afterFrom = changeDay.AddDays(1);
        DateOnly afterTo = afterFrom.AddDays(windowWeeks * 7 - 1);
        DateOnly beforeTo = changeDay.AddDays(-1);
        DateOnly beforeFrom = beforeTo.AddDays(-(windowWeeks * 7) + 1);

        GroupWindow Sum(bool test, DateOnly from, DateOnly to)
        {
            List<DailyPage> rows = days
                .Where(d => d.Date >= from && d.Date <= to && testPages.Contains(d.Page) == test)
                .ToList();
            return new GroupWindow(rows.Sum(d => d.Clicks), rows.Sum(d => d.Impressions));
        }

        double WeekRatio(DateOnly weekStart)
        {
            GroupWindow test = Sum(true, weekStart, weekStart.AddDays(6));
            GroupWindow control = Sum(false, weekStart, weekStart.AddDays(6));
            return (double)test.Clicks / control.Clicks;
        }

        GroupWindow testBefore = Sum(true, beforeFrom, beforeTo);
        GroupWindow testAfter = Sum(true, afterFrom, afterTo);
        GroupWindow controlBefore = Sum(false, beforeFrom, beforeTo);
        GroupWindow controlAfter = Sum(false, afterFrom, afterTo);

        // Test clicks relative to control clicks: shared effects (season, holiday, update) cancel out.
        double effect = (double)testAfter.Clicks / testBefore.Clicks
                        / ((double)controlAfter.Clicks / controlBefore.Clicks) - 1;

        List<double> baseline = Enumerable.Range(1, baselineWeeks)
            .Select(w => WeekRatio(changeDay.AddDays(-7 * w)))
            .ToList();
        List<double> after = Enumerable.Range(0, windowWeeks)
            .Select(w => WeekRatio(afterFrom.AddDays(7 * w)))
            .ToList();

        return new ImpactResult(testBefore, testAfter, controlBefore, controlAfter, effect, baseline, after);
    }
}
