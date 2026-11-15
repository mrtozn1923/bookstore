namespace Bookstore.Analytics.Web.Analytics;

public sealed class AnalyticsOptions
{
    public const string SectionName = "Analytics";

    // G-... measurement ID of the GA4 web data stream; empty = no tag on the page
    public string? MeasurementId { get; init; }

    // Measurement Protocol secret; comes from an environment variable, never from the repository
    public string? ApiSecret { get; init; }

    public Uri Endpoint { get; init; } = new("https://www.google-analytics.com/");
}
