namespace Bookstore.Analytics.SearchConsole;

public sealed record SearchAnalyticsRow(
    DateOnly Date,
    string SearchType,
    string Page,
    string Query,
    string Country,
    string Device,
    int Clicks,
    int Impressions,
    double Position);

public sealed record DailyTotal(DateOnly Date, string SearchType, int Clicks, int Impressions, double Position);
