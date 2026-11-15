namespace Bookstore.Analytics.Rum;

public sealed record WebVitalSample(string Id, string Metric, double Value, string PageGroup, string FormFactor);

public sealed record MetricSummary(string Metric, int Samples, double P75, Rating Rating);

public sealed record GroupSummary(string PageGroup, string FormFactor, IReadOnlyList<MetricSummary> Metrics, Rating? Status)
{
    public bool HasEnoughData => Status is not null;
}

public static class RumReport
{
    // Like the Search Console report: a group needs enough LCP and CLS data,
    // and its status is its worst metric.
    public static List<GroupSummary> Build(IEnumerable<WebVitalSample> samples, int minSamples = 50) =>
        samples
            .GroupBy(s => (s.PageGroup, s.FormFactor))
            .OrderBy(g => g.Key.PageGroup, StringComparer.Ordinal).ThenBy(g => g.Key.FormFactor, StringComparer.Ordinal)
            .Select(g =>
            {
                List<MetricSummary> metrics = CoreWebVitals.Metrics
                    .Select(m => g.Where(s => s.Metric == m).Select(s => s.Value).ToList())
                    .Zip(CoreWebVitals.Metrics)
                    .Where(x => x.First.Count > 0)
                    .Select(x =>
                    {
                        double p75 = Percentile.NearestRank(x.First, 0.75);
                        return new MetricSummary(x.Second, x.First.Count, p75, CoreWebVitals.Rate(x.Second, p75));
                    })
                    .ToList();

                bool enough = metrics.Count(m => m.Metric is "LCP" or "CLS" && m.Samples >= minSamples) == 2;
                Rating? status = enough ? metrics.Max(m => m.Rating) : null;
                return new GroupSummary(g.Key.PageGroup, g.Key.FormFactor, metrics, status);
            })
            .ToList();
}
