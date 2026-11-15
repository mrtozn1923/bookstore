using Bookstore.Analytics.Archive;
using Bookstore.Analytics.SearchConsole;
using Google.Apis.SearchConsole.v1;

namespace Bookstore.Analytics.Cli;

public static class FetchCommand
{
    // Search Console dates are Pacific Time; final data is usually ready after 2-3 days.
    public static DateOnly LatestFinalDay(DateTimeOffset now)
    {
        TimeZoneInfo pacific = TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, pacific).DateTime).AddDays(-3);
    }

    public static async Task<int> RunAsync(string site, DateOnly from, DateOnly to, Options options)
    {
        using SearchConsoleService service = options.StubUri is { } stub
            ? SearchConsoleClient.CreateForStub(stub)
            : await SearchConsoleClient.CreateAsync(options.KeyFilePath
                ?? throw new InvalidOperationException("Set BOOKSTORE_GSC_KEY_FILE to the service account key path."));

        SearchAnalyticsFetcher fetcher = new(service, options.PageSize);
        SqlServerArchive archive = new(options.ConnectionString);
        await archive.EnsureSchemaAsync();

        for (DateOnly day = from; day <= to; day = day.AddDays(1))
        {
            DailyTotal total = await fetcher.FetchTotalAsync(site, day);
            List<SearchAnalyticsRow> rows = await fetcher.FetchDetailAsync(site, day);
            await archive.ReplaceDayAsync(total, rows);
            Console.WriteLine($"{day:yyyy-MM-dd} web: {rows.Count,3} detail rows, {rows.Sum(r => r.Clicks),4} clicks in rows, {total.Clicks,4} clicks in total");
        }

        return 0;
    }

    public sealed record Options(string ConnectionString, string? KeyFilePath, Uri? StubUri, int PageSize);
}
