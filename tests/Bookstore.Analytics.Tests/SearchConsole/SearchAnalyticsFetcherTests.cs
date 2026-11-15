using System.Net;
using System.Text.Json.Nodes;
using Bookstore.Analytics.Cli;
using Bookstore.Analytics.SearchConsole;
using Bookstore.Analytics.Tests.Fakes;
using Google.Apis.SearchConsole.v1;
using Google.Apis.Services;

namespace Bookstore.Analytics.Tests.SearchConsole;

public class SearchAnalyticsFetcherTests
{
    private static SearchConsoleService Service(FakeHandler handler) =>
        new(new BaseClientService.Initializer
        {
            HttpClientFactory = new FakeGoogleHttpClientFactory(handler),
            ApplicationName = "tests",
        });

    private static string Rows(int count) => new JsonObject
    {
        ["rows"] = new JsonArray(Enumerable.Range(0, count).Select(i => (JsonNode)new JsonObject
        {
            ["keys"] = new JsonArray("/blog/p" + i, "q" + i, "tur", "MOBILE"),
            ["clicks"] = 1,
            ["impressions"] = 10,
            ["ctr"] = 0.1,
            ["position"] = 3.5,
        }).ToArray()),
    }.ToJsonString();

    [Fact]
    public async Task Pages_with_startRow_until_an_empty_response()
    {
        FakeHandler handler = new((_, body) =>
        {
            int startRow = (int)JsonNode.Parse(body)!["startRow"]!;
            return (HttpStatusCode.OK, startRow switch { 0 => Rows(2), 2 => Rows(1), _ => "{}" });
        });
        SearchAnalyticsFetcher fetcher = new(Service(handler), pageSize: 2);

        List<SearchAnalyticsRow> rows = await fetcher.FetchDetailAsync("sc-domain:bookstore.example", new DateOnly(2026, 11, 9));

        Assert.Equal(3, rows.Count);
        Assert.Equal([0, 2, 4], handler.Calls.Select(c => (int)JsonNode.Parse(c.Body)!["startRow"]!));
        JsonNode first = JsonNode.Parse(handler.Calls[0].Body)!;
        Assert.Equal("2026-11-09", (string)first["startDate"]!);
        Assert.Equal("2026-11-09", (string)first["endDate"]!);
        Assert.Equal("final", (string)first["dataState"]!);
        Assert.EndsWith("/webmasters/v3/sites/sc-domain%3Abookstore.example/searchAnalytics/query",
            handler.Calls[0].Request.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Total_query_sends_no_page_or_query_dimension()
    {
        FakeHandler handler = new((_, _) => (HttpStatusCode.OK,
            """{"rows":[{"clicks":120,"impressions":3000,"ctr":0.04,"position":7.4}]}"""));
        SearchAnalyticsFetcher fetcher = new(Service(handler));

        DailyTotal total = await fetcher.FetchTotalAsync("sc-domain:bookstore.example", new DateOnly(2026, 11, 9));

        Assert.Equal(120, total.Clicks);
        JsonNode body = JsonNode.Parse(handler.Calls.Single().Body)!;
        Assert.True(body["dimensions"] is null || body["dimensions"]!.AsArray().Count == 0);
    }

    [Fact]
    public void Latest_final_day_is_three_days_back_in_pacific_time()
    {
        // 2026-11-15 05:00 UTC is still 2026-11-14 in Los Angeles
        DateOnly day = FetchCommand.LatestFinalDay(new DateTimeOffset(2026, 11, 15, 5, 0, 0, TimeSpan.Zero));
        Assert.Equal(new DateOnly(2026, 11, 11), day);
    }
}
