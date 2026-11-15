using System.Globalization;
using Google.Apis.SearchConsole.v1;
using Google.Apis.SearchConsole.v1.Data;

namespace Bookstore.Analytics.SearchConsole;

public sealed class SearchAnalyticsFetcher(SearchConsoleService service, int pageSize = 25_000)
{
    private static readonly string[] DetailDimensions = ["page", "query", "country", "device"];

    // Page + query rows of one day; Google may drop some rows at this level of detail.
    public async Task<List<SearchAnalyticsRow>> FetchDetailAsync(
        string siteUrl, DateOnly day, string type = "web", CancellationToken ct = default)
    {
        List<SearchAnalyticsRow> rows = [];
        for (int startRow = 0; ; startRow += pageSize)
        {
            SearchAnalyticsQueryResponse response = await QueryAsync(siteUrl, day, type, DetailDimensions, startRow, ct);
            if (response.Rows is not { Count: > 0 } page)
            {
                return rows;
            }

            rows.AddRange(page.Select(r => new SearchAnalyticsRow(
                day, type, r.Keys[0], r.Keys[1], r.Keys[2], r.Keys[3],
                (int)(r.Clicks ?? 0), (int)(r.Impressions ?? 0), r.Position ?? 0)));
        }
    }

    // Without page and query dimensions the totals are complete.
    public async Task<DailyTotal> FetchTotalAsync(
        string siteUrl, DateOnly day, string type = "web", CancellationToken ct = default)
    {
        SearchAnalyticsQueryResponse response = await QueryAsync(siteUrl, day, type, [], 0, ct);
        ApiDataRow? row = response.Rows?.FirstOrDefault();
        return new DailyTotal(day, type, (int)(row?.Clicks ?? 0), (int)(row?.Impressions ?? 0), row?.Position ?? 0);
    }

    private Task<SearchAnalyticsQueryResponse> QueryAsync(
        string siteUrl, DateOnly day, string type, IList<string> dimensions, int startRow, CancellationToken ct)
    {
        string date = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        SearchAnalyticsQueryRequest body = new()
        {
            StartDate = date,
            EndDate = date,
            Type = type,
            Dimensions = dimensions,
            DataState = "final",
            RowLimit = pageSize,
            StartRow = startRow,
        };
        return service.Searchanalytics.Query(body, siteUrl).ExecuteAsync(ct);
    }
}
