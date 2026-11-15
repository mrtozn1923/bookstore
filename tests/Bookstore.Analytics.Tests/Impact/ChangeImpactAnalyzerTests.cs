using Bookstore.Analytics.Impact;

namespace Bookstore.Analytics.Tests.Impact;

public class ChangeImpactAnalyzerTests
{
    private static readonly DateOnly Change = new(2026, 10, 14);

    // 16 weeks of two pages; after the change the test page doubles its clicks
    // and every page gets a 50% seasonal lift.
    private static List<DailyPage> Days(bool effect)
    {
        List<DailyPage> days = [];
        for (DateOnly d = Change.AddDays(-12 * 7); d <= Change.AddDays(28); d = d.AddDays(1))
        {
            double season = d > Change ? 1.5 : 1.0;
            int wobble = d.DayNumber % 3;
            days.Add(new DailyPage(d, "/test", (int)((100 + wobble) * season * (effect && d > Change ? 2 : 1)), 2000));
            days.Add(new DailyPage(d, "/control", (int)((200 + wobble) * season), 4000));
        }

        return days;
    }

    [Fact]
    public void Control_group_removes_the_shared_seasonal_lift()
    {
        ImpactResult r = ChangeImpactAnalyzer.Analyze(Days(effect: false), new HashSet<string> { "/test" }, Change);

        Assert.True(r.TestAfter.Clicks > r.TestBefore.Clicks * 1.4); // naive before/after: +50%
        Assert.Equal(0, r.RelativeEffect, 2);
        Assert.False(r.OutsideNormalVariation);
    }

    [Fact]
    public void A_real_effect_is_outside_normal_variation()
    {
        ImpactResult r = ChangeImpactAnalyzer.Analyze(Days(effect: true), new HashSet<string> { "/test" }, Change);

        Assert.Equal(1.0, r.RelativeEffect, 1);
        Assert.True(r.OutsideNormalVariation);
    }

    [Fact]
    public void Windows_are_whole_weeks_without_the_change_day()
    {
        ImpactResult r = ChangeImpactAnalyzer.Analyze(Days(effect: false), new HashSet<string> { "/test" }, Change);
        Assert.Equal(4, r.AfterWeeklyRatios.Count);
        Assert.Equal(12, r.BaselineWeeklyRatios.Count);
    }
}
