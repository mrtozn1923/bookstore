namespace Bookstore.Api.Rum;

// "good" and "poor" boundaries from web.dev; a value is rated at the 75th percentile
public static class RumThresholds
{
    private static readonly Dictionary<string, (double Good, double Poor)> Limits = new()
    {
        ["TTFB"] = (800, 1800),
        ["FCP"] = (1800, 3000),
        ["LCP"] = (2500, 4000),
        ["INP"] = (200, 500),
        ["CLS"] = (0.1, 0.25),
    };

    public static bool IsKnown(string metric) => Limits.ContainsKey(metric);

    public static string Rate(string metric, double value)
    {
        (double good, double poor) = Limits[metric];
        return value <= good ? "good" : value <= poor ? "needs-improvement" : "poor";
    }
}
