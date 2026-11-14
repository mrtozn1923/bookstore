using System.Collections.Concurrent;

namespace Bookstore.Api.Rum;

public sealed class RumStore(TimeProvider clock)
{
    private const int MaxSamples = 100_000;
    private readonly ConcurrentQueue<RumSample> _samples = new();

    public void Add(RumReport report)
    {
        _samples.Enqueue(new RumSample(report, clock.GetUtcNow()));
        // Keep memory bounded: drop the oldest samples instead of growing forever
        while (_samples.Count > MaxSamples && _samples.TryDequeue(out _))
        {
        }
    }

    public IReadOnlyList<RumRow> P75(TimeSpan window, int minSamples)
    {
        DateTimeOffset since = clock.GetUtcNow() - window;
        return _samples
            .Where(s => s.ReceivedAt >= since)
            .GroupBy(s => (s.Report.Route, s.Report.Name, s.Report.Device))
            .Select(g =>
            {
                double[] values = g.Select(s => s.Report.Value).Order().ToArray();
                double p75 = Percentile(values, 0.75);
                string rating = RumThresholds.Rate(g.Key.Name, p75);
                bool alarm = values.Length >= minSamples && rating != "good";
                return new RumRow(g.Key.Route, g.Key.Name, g.Key.Device, values.Length, Math.Round(p75, 3), rating, alarm);
            })
            .OrderBy(r => r.Route).ThenBy(r => r.Metric).ThenBy(r => r.Device)
            .ToList();
    }

    // Nearest-rank percentile on a sorted array
    private static double Percentile(double[] sorted, double p)
    {
        int rank = (int)Math.Ceiling(p * sorted.Length);
        return sorted[Math.Clamp(rank - 1, 0, sorted.Length - 1)];
    }
}
