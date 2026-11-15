using Bookstore.Analytics.Rum;

namespace Bookstore.Analytics.Tests.Rum;

public class RumReportTests
{
    [Theory]
    [InlineData("LCP", 2500, Rating.Good)]
    [InlineData("LCP", 2501, Rating.NeedsImprovement)]
    [InlineData("LCP", 4001, Rating.Poor)]
    [InlineData("INP", 200, Rating.Good)]
    [InlineData("INP", 500, Rating.NeedsImprovement)]
    [InlineData("CLS", 0.1, Rating.Good)]
    [InlineData("CLS", 0.26, Rating.Poor)]
    public void Thresholds(string metric, double value, Rating rating) =>
        Assert.Equal(rating, CoreWebVitals.Rate(metric, value));

    [Fact]
    public void Nearest_rank_p75()
    {
        Assert.Equal(3, Percentile.NearestRank([4, 1, 3, 2], 0.75));
        Assert.Equal(8, Percentile.NearestRank([1, 2, 3, 4, 5, 6, 7, 8, 9, 10], 0.75));
        Assert.Equal(5, Percentile.NearestRank([5], 0.75));
    }

    private static IEnumerable<WebVitalSample> Samples(string metric, int count, double value, string group = "/blog/:slug") =>
        Enumerable.Range(0, count).Select(i => new WebVitalSample($"{metric}-{group}-{i}", metric, value, group, "phone"));

    [Fact]
    public void Group_status_is_the_worst_metric()
    {
        GroupSummary g = Assert.Single(RumReport.Build(
            [.. Samples("LCP", 60, 1800), .. Samples("CLS", 60, 0.3), .. Samples("INP", 60, 150)]));

        Assert.Equal(Rating.Poor, g.Status);
        Assert.Equal(Rating.Good, g.Metrics.Single(m => m.Metric == "LCP").Rating);
    }

    [Fact]
    public void Missing_cls_data_means_no_status()
    {
        GroupSummary g = Assert.Single(RumReport.Build([.. Samples("LCP", 60, 1800), .. Samples("CLS", 10, 0.01)]));
        Assert.False(g.HasEnoughData);
    }
}
