using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Bookstore.Analytics.Tests.Web;

public class GoogleTagTests
{
    [Fact]
    public async Task Inline_tag_script_carries_the_nonce_from_the_csp_header()
    {
        using WebAppFactory factory = new();
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/");
        string html = await response.Content.ReadAsStringAsync();

        string csp = response.Headers.GetValues("Content-Security-Policy").Single();
        string nonce = Regex.Match(csp, "'nonce-([^']+)'").Groups[1].Value;
        Assert.NotEmpty(nonce);
        Assert.Contains($"<script nonce=\"{nonce}\">", html);
        Assert.Contains("gtag('config', 'G-TEST123');", html);
    }

    [Fact]
    public async Task Every_response_gets_a_new_nonce()
    {
        using WebAppFactory factory = new();
        HttpClient client = factory.CreateClient();
        string first = (await client.GetAsync("/")).Headers.GetValues("Content-Security-Policy").Single();
        string second = (await client.GetAsync("/")).Headers.GetValues("Content-Security-Policy").Single();
        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task No_measurement_id_means_no_tag()
    {
        using WebAppFactory factory = new(measurementId: null);
        string html = await factory.CreateClient().GetStringAsync("/");
        Assert.DoesNotContain("googletagmanager", html);
    }

    [Fact]
    public async Task Pdf_download_sends_a_measurement_protocol_event_with_the_client_id()
    {
        using WebAppFactory factory = new();
        HttpClient client = factory.CreateClient();
        HttpRequestMessage request = new(HttpMethod.Get, "/downloads/analytics-ve-olcum.pdf");
        request.Headers.Add("Cookie", "bs_ga_cid=123456789.1700000000");

        HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType!.MediaType);
        (HttpRequestMessage sent, string body) = Assert.Single(factory.Ga4.Calls);
        Assert.Equal("/mp/collect", sent.RequestUri!.AbsolutePath);
        Assert.Contains("measurement_id=G-TEST123", sent.RequestUri.Query);
        JsonNode json = JsonNode.Parse(body)!;
        Assert.Equal("123456789.1700000000", (string)json["client_id"]!);
        Assert.Equal("pdf_download_served", (string)json["events"]![0]!["name"]!);
        Assert.Equal("analytics-ve-olcum.pdf", (string)json["events"]![0]!["params"]!["file_name"]!);
    }

    [Fact]
    public async Task Pdf_download_without_client_id_sends_nothing()
    {
        using WebAppFactory factory = new();
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/downloads/analytics-ve-olcum.pdf");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(factory.Ga4.Calls);
    }
}
