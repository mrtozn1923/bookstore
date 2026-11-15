using System.Net;
using Bookstore.Analytics.Crux;
using Bookstore.Analytics.Tests.Fakes;

namespace Bookstore.Analytics.Tests.Rum;

public class CruxClientTests
{
    // shape from the CrUX API documentation; the numbers are made up
    private const string Response = """
        {"record":{"key":{"formFactor":"PHONE","origin":"https://www.bookstore.example"},
         "metrics":{
          "largest_contentful_paint":{"histogram":[{"start":0,"end":2500,"density":0.71}],"percentiles":{"p75":2710}},
          "interaction_to_next_paint":{"percentiles":{"p75":180}},
          "cumulative_layout_shift":{"percentiles":{"p75":"0.05"}}},
         "collectionPeriod":{"firstDate":{"year":2026,"month":10,"day":16},"lastDate":{"year":2026,"month":11,"day":12}}}}
        """;

    [Fact]
    public async Task Parses_p75_values_and_the_collection_period()
    {
        FakeHandler handler = new((_, _) => (HttpStatusCode.OK, Response));
        CruxClient client = new(new HttpClient(handler) { BaseAddress = new Uri("https://chromeuxreport.googleapis.com/") },
            "fake-api-key-for-tests");

        CruxRecord? record = await client.QueryOriginAsync("https://www.bookstore.example", "PHONE");

        Assert.Equal(new CruxRecord(2710, 180, 0.05, new DateOnly(2026, 10, 16), new DateOnly(2026, 11, 12)), record);
        Assert.Equal("/v1/records:queryRecord", handler.Calls[0].Request.RequestUri!.AbsolutePath);
        Assert.Contains("\"formFactor\":\"PHONE\"", handler.Calls[0].Body);
    }

    [Fact]
    public async Task No_data_returns_null()
    {
        FakeHandler handler = new((_, _) => (HttpStatusCode.NotFound, """{"error":{"code":404}}"""));
        CruxClient client = new(new HttpClient(handler) { BaseAddress = new Uri("https://chromeuxreport.googleapis.com/") },
            "fake-api-key-for-tests");

        Assert.Null(await client.QueryOriginAsync("https://small-site.example", "PHONE"));
    }
}
