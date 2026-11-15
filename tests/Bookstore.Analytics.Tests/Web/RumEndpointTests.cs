using System.Net;
using System.Net.Http.Json;

namespace Bookstore.Analytics.Tests.Web;

public class RumEndpointTests
{
    private const string Phone = "Mozilla/5.0 (Linux; Android 15) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 Mobile Safari/537.36";

    [Fact]
    public async Task Stores_valid_beacons_and_reports_by_group_and_form_factor()
    {
        using WebAppFactory factory = new();
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(Phone);

        object[] beacons =
        [
            new { id = "v5-1", name = "LCP", value = 2100.0, pageGroup = "/blog/:slug" },
            new { id = "v5-2", name = "CLS", value = 0.02, pageGroup = "/blog/:slug" },
            new { id = "v5-3", name = "FID", value = 10.0, pageGroup = "/blog/:slug" },
            new { id = "v5-4", name = "LCP", value = -5.0, pageGroup = "/blog/:slug" },
            new { id = "v5-5", name = "LCP", value = 900.0, pageGroup = "/blog/dune" },
        ];
        HttpResponseMessage post = await client.PostAsJsonAsync("/rum", beacons);
        string report = await client.GetStringAsync("/rum/report");

        Assert.Equal(HttpStatusCode.NoContent, post.StatusCode);
        Assert.Contains("/blog/:slug [phone] -> not enough data", report);
        Assert.Contains("LCP  p75   2100 ms", report);
        Assert.Contains("other [phone]", report);
        Assert.DoesNotContain("FID", report);
    }
}
