using System.Collections.Concurrent;
using System.Text;
using Bookstore.Analytics.Rum;

namespace Bookstore.Analytics.Web.Rum;

// What the browser sends: the web-vitals metric plus the route pattern.
public sealed record RumBeacon(string Id, string Name, double Value, string PageGroup);

// Demo store: last value per metric id (web-vitals may report a metric again with the same id).
public sealed class RumStore
{
    private const int MaxSamples = 100_000;
    private readonly ConcurrentDictionary<string, WebVitalSample> samples = new();

    public void Add(WebVitalSample sample)
    {
        if (samples.Count < MaxSamples || samples.ContainsKey(sample.Id))
        {
            samples[sample.Id] = sample;
        }
    }

    public IEnumerable<WebVitalSample> All => samples.Values;
}

public static class RumEndpoints
{
    // Known route patterns only; free-form URLs would split the data into groups too small for a p75.
    private static readonly HashSet<string> PageGroups = new(["/", "/blog/:slug"], StringComparer.Ordinal);

    public static IEndpointRouteBuilder MapRum(this IEndpointRouteBuilder app)
    {
        app.MapPost("/rum", (RumBeacon[] beacons, HttpRequest request, RumStore store) =>
        {
            string formFactor = FormFactor(request.Headers.UserAgent.ToString());
            foreach (RumBeacon b in beacons.Take(10))
            {
                if (IsValid(b))
                {
                    store.Add(new WebVitalSample(b.Id, b.Name, b.Value,
                        PageGroups.Contains(b.PageGroup) ? b.PageGroup : "other", formFactor));
                }
            }

            return Results.NoContent();
        });

        app.MapGet("/rum/report", (RumStore store) =>
        {
            StringBuilder text = new();
            foreach (GroupSummary g in RumReport.Build(store.All))
            {
                string status = g.Status?.ToString() ?? "not enough data";
                text.AppendLine($"{g.PageGroup} [{g.FormFactor}] -> {status}");
                foreach (MetricSummary m in g.Metrics)
                {
                    string p75 = m.Metric == "CLS" ? m.P75.ToString("0.00") : $"{m.P75:0} ms";
                    text.AppendLine($"    {m.Metric,-4} p75 {p75,9}  {m.Rating,-16} n={m.Samples}");
                }
            }

            return Results.Text(text.ToString());
        });

        return app;
    }

    private static bool IsValid(RumBeacon b) =>
        b.Id is { Length: > 0 and <= 64 } && double.IsFinite(b.Value) && b.Value >= 0 && b.Name switch
        {
            "LCP" or "INP" => b.Value <= 60_000,
            "CLS" => b.Value <= 10,
            _ => false,
        };

    // CrUX also splits phone, tablet and desktop; a rough User-Agent check is enough here.
    public static string FormFactor(string userAgent) =>
        userAgent.Contains("iPad", StringComparison.Ordinal) || userAgent.Contains("Tablet", StringComparison.Ordinal) ? "tablet"
        : userAgent.Contains("Mobi", StringComparison.Ordinal) ? "phone"
        : "desktop";
}
