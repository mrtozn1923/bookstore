using System.Text.Json;
using System.Text.Json.Nodes;

// A local stand-in for the Search Analytics endpoint that serves synthetic data.
// It is not Google: it only mimics the request and response shape.
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
// the Google client gzips request bodies
builder.Services.AddRequestDecompression();
WebApplication app = builder.Build();
app.UseRequestDecompression();

app.MapPost("/webmasters/v3/sites/{site}/searchAnalytics/query", async (string site, HttpRequest request) =>
{
    JsonNode body = (await JsonNode.ParseAsync(request.Body))!;
    DateOnly day = DateOnly.Parse((string)body["startDate"]!);
    string[] dimensions = body["dimensions"]?.AsArray().Select(d => (string)d!).ToArray() ?? [];
    int startRow = (int?)body["startRow"] ?? 0;
    int rowLimit = (int?)body["rowLimit"] ?? 1000;
    app.Logger.LogInformation("query site={Site} date={Day} dims=[{Dims}] startRow={StartRow} rowLimit={RowLimit}",
        site, day.ToString("yyyy-MM-dd"), string.Join(',', dimensions), startRow, rowLimit);

    List<SyntheticRow> all = SyntheticData.For(day);
    if (dimensions.Length == 0)
    {
        // totals include clicks of anonymized queries that detail rows do not show
        int clicks = (int)Math.Round(all.Sum(r => r.Clicks) * 1.08);
        int impressions = (int)Math.Round(all.Sum(r => r.Impressions) * 1.15);
        return Results.Json(new { rows = new[] { new { clicks, impressions, ctr = (double)clicks / impressions, position = 7.4 } }, responseAggregationType = "byProperty" });
    }

    var rows = all.Skip(startRow).Take(rowLimit).Select(r => new
    {
        keys = new[] { r.Page, r.Query, r.Country, r.Device },
        clicks = r.Clicks,
        impressions = r.Impressions,
        ctr = (double)r.Clicks / r.Impressions,
        position = r.Position,
    }).ToList();
    return rows.Count == 0
        ? Results.Json(new { responseAggregationType = "byPage" })
        : Results.Json(new { rows, responseAggregationType = "byPage" });
});

app.Run();

internal sealed record SyntheticRow(string Page, string Query, string Country, string Device, int Clicks, int Impressions, double Position);

internal static class SyntheticData
{
    private static readonly (string Page, string Query, int BaseClicks)[] Pages =
    [
        ("/blog/ef-core-migration-rehberi", "ef core migration", 12),
        ("/blog/ef-core-migration-rehberi", "ef core migration geri alma", 4),
        ("/blog/cs8618-nullable-uyarisi", "cs8618", 8),
        ("/blog/cs8618-nullable-uyarisi", "non-nullable property must contain", 4),
        ("/blog/dependency-injection-nedir", "dependency injection nedir", 3),
        ("/blog/async-await-nasil-calisir", "async await nasıl çalışır", 2),
        ("/blog/ef-core-vs-dapper", "ef core vs dapper", 2),
    ];

    public static List<SyntheticRow> For(DateOnly day)
    {
        bool secondWeek = day >= new DateOnly(2026, 11, 5);
        double weekday = day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 0.6 : 1.0;
        List<SyntheticRow> rows = [];
        foreach ((string page, string query, int baseClicks) in Pages)
        {
            double trend = (secondWeek, page) switch
            {
                (true, "/blog/ef-core-migration-rehberi") => 0.55,
                (true, "/blog/cs8618-nullable-uyarisi") => 1.35,
                _ => 1.0,
            };
            foreach ((string country, double share) in new[] { ("tur", 0.8), ("usa", 0.2) })
            {
                foreach ((string device, double split) in new[] { ("MOBILE", 0.45), ("DESKTOP", 0.55) })
                {
                    int clicks = (int)Math.Round(baseClicks * weekday * trend * share * split * 4);
                    rows.Add(new SyntheticRow(page, query, country, device, clicks, Math.Max(1, clicks * 12 + 5), 3.2));
                }
            }
        }

        return rows;
    }
}
