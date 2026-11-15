using System.Net;

namespace Bookstore.Analytics.Tests.Web;

public class ConsentTests
{
    [Fact]
    public async Task Without_consent_there_is_no_tag_and_no_cookie()
    {
        using WebAppFactory factory = new();
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/");

        Assert.DoesNotContain("googletagmanager", await response.Content.ReadAsStringAsync());
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Without_consent_the_pdf_is_served_but_nothing_goes_to_ga4()
    {
        using WebAppFactory factory = new();
        HttpRequestMessage request = new(HttpMethod.Get, "/downloads/analytics-ve-olcum.pdf");
        request.Headers.Add("Cookie", "bs_ga_cid=123456789.1700000000");

        HttpResponseMessage response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(factory.Ga4.Calls);
    }

    [Fact]
    public async Task Grant_writes_the_consent_cookie()
    {
        using WebAppFactory factory = new();
        HttpResponseMessage response = await factory.CreateClient().PostAsync("/consent/analytics/grant", null);

        string cookie = response.Headers.GetValues("Set-Cookie").Single();
        Assert.StartsWith(".AspNet.Consent=yes", cookie);
    }

    [Fact]
    public async Task Withdraw_deletes_consent_and_analytics_cookies()
    {
        using WebAppFactory factory = new();
        HttpRequestMessage request = new(HttpMethod.Post, "/consent/analytics/withdraw");
        request.Headers.Add("Cookie", ".AspNet.Consent=yes; _ga=GA1.1.1.1; _ga_TEST123=GS2.1.1; bs_ga_cid=1.1; theme=dark");

        HttpResponseMessage response = await factory.CreateClient().SendAsync(request);

        string[] deleted = response.Headers.GetValues("Set-Cookie").Select(c => c.Split("=")[0]).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal([".AspNet.Consent", "_ga", "_ga_TEST123", "bs_ga_cid"], deleted);
    }
}
