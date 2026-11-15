using Bookstore.Analytics.Consent;

namespace Bookstore.Analytics.Tests.Consent;

public class CoverageEstimatorTests
{
    [Fact]
    public void Groups_whole_weeks_and_drops_the_partial_one()
    {
        DateOnly start = new(2026, 10, 12);
        IEnumerable<CoverageDay> days = Enumerable.Range(0, 10)
            .Select(i => new CoverageDay(start.AddDays(i), 100, i < 7 ? 60 : 40));

        List<CoverageWeek> weeks = CoverageEstimator.Weekly(days);

        CoverageWeek week = Assert.Single(weeks);
        Assert.Equal(start, week.From);
        Assert.Equal(0.6, week.Coverage, 3);
    }
}
