namespace Bookstore.Analytics.Rum;

public enum Rating
{
    Good,
    NeedsImprovement,
    Poor,
}

// Thresholds of the Search Console Core Web Vitals report (good is "<=").
public static class CoreWebVitals
{
    public static readonly string[] Metrics = ["LCP", "INP", "CLS"];

    public static Rating Rate(string metric, double value)
    {
        (double good, double poor) = metric switch
        {
            "LCP" => (2500d, 4000d),
            "INP" => (200d, 500d),
            "CLS" => (0.1, 0.25),
            _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, "Not a Core Web Vital."),
        };
        return value <= good ? Rating.Good : value <= poor ? Rating.NeedsImprovement : Rating.Poor;
    }
}

public static class Percentile
{
    // Nearest-rank: the smallest value with at least p of the samples at or below it.
    public static double NearestRank(IReadOnlyList<double> values, double p)
    {
        if (values.Count == 0)
        {
            throw new ArgumentException("No samples.", nameof(values));
        }

        double[] sorted = [.. values.Order()];
        int rank = (int)Math.Ceiling(p * sorted.Length);
        return sorted[Math.Max(rank, 1) - 1];
    }
}
