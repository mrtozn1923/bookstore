using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Bookstore.Web.Tests;

public sealed class CatalogFacetTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Theory]
    [InlineData("/books/genre/science-fiction", 200, null)]
    [InlineData("/books/genre/science-fiction?page=3", 200, null)]
    [InlineData("/books/genre/science-fiction/tr", 200, null)]
    [InlineData("/books/genre/science-fiction?page=1", 301, "/books/genre/science-fiction")]
    [InlineData("/books/genre/science-fiction?sort=title", 301, "/books/genre/science-fiction")]
    [InlineData("/books/genre/science-fiction?page=2&format=hardcover", 404, null)] // 10 kitap, tek sayfa
    [InlineData("/books/genre/science-fiction?sort=year&format=ebook", 301, "/books/genre/science-fiction?format=ebook&sort=year")]
    [InlineData("/books/genre/science-fiction?page=4", 404, null)]
    [InlineData("/books/genre/science-fiction?page=0", 404, null)]
    [InlineData("/books/genre/science-fiction?format=ebook&format=ebook", 404, null)]
    [InlineData("/books/genre/science-fiction?format=vinyl", 404, null)]
    [InlineData("/books/genre/fantasy/tr", 404, null)] // Türkçe fantastik kitap yok
    [InlineData("/books/genre/science-fiction/tr/tr", 404, null)]
    [InlineData("/books/genre/horror", 404, null)]
    public async Task Facet_urls_return_the_right_status(string url, int status, string? location)
    {
        HttpResponseMessage response = await _client.GetAsync(url);
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal(location, response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Paginated_page_is_self_canonical_and_links_sequentially()
    {
        IDocument doc = new HtmlParser().ParseDocument(await _client.GetStringAsync("/books/genre/science-fiction?page=2"));
        string[] hrefs = [.. doc.QuerySelectorAll("nav a").Select(a => a.GetAttribute("href")!)];

        Assert.Equal("https://bookstore.example/books/genre/science-fiction?page=2", doc.QuerySelector("link[rel=canonical]")!.GetAttribute("href"));
        Assert.Contains("/books/genre/science-fiction", hrefs);        // ilk sayfa
        Assert.Contains("/books/genre/science-fiction?page=3", hrefs); // sonraki
        Assert.DoesNotContain(hrefs, h => h.Contains('#'));
        Assert.Empty(doc.QuerySelectorAll("link[rel=next], link[rel=prev]")); // Google artık kullanmıyor
        Assert.Equal(12, doc.QuerySelectorAll("main ul > li").Length);
    }

    [Fact]
    public async Task Filter_links_are_normalized_catalog_urls()
    {
        IDocument doc = new HtmlParser().ParseDocument(await _client.GetStringAsync("/books/genre/science-fiction?page=2"));
        string[] filters = [.. doc.QuerySelectorAll("main p a").Select(a => a.GetAttribute("href")!).Where(h => h.Contains('?'))];
        Assert.Equal(["/books/genre/science-fiction?format=hardcover", "/books/genre/science-fiction?format=paperback",
            "/books/genre/science-fiction?format=ebook", "/books/genre/science-fiction?sort=price"], filters);
    }

    [Fact]
    public async Task Language_filter_that_does_not_narrow_the_list_points_to_the_parent()
    {
        // fantastik türünde yalnız İngilizce kitap var: /fantasy/en, /fantasy ile aynı liste
        IDocument doc = new HtmlParser().ParseDocument(await _client.GetStringAsync("/books/genre/fantasy/en"));
        Assert.Equal("https://bookstore.example/books/genre/fantasy", doc.QuerySelector("link[rel=canonical]")!.GetAttribute("href"));
    }

    [Theory]
    [InlineData("/books/genre/science-fiction?page=2", true)]
    [InlineData("/books/genre/science-fiction/tr", true)]
    [InlineData("/books/genre/science-fiction?format=ebook", false)]
    [InlineData("/books/genre/science-fiction?format=ebook&sort=price", false)]
    [InlineData("/books/genre/science-fiction?page=2&sort=year", false)]
    public async Task Robots_txt_blocks_only_non_indexable_filters(string url, bool allowed)
    {
        string robots = await _client.GetStringAsync("/robots.txt");
        // RFC 9309: * herhangi bir karakter dizisi; en uzun eşleşen kural kazanır (burada yalnız Disallow var)
        bool blocked = robots.Split('\n')
            .Where(l => l.StartsWith("Disallow:"))
            .Select(l => "^" + Regex.Escape(l["Disallow:".Length..].Trim()).Replace(@"\*", ".*"))
            .Any(pattern => Regex.IsMatch(url, pattern));
        Assert.Equal(allowed, !blocked);
    }

    [Fact]
    public async Task Every_url_in_the_genre_sitemap_returns_200_with_itself_as_canonical()
    {
        XNamespace sm = "http://www.sitemaps.org/schemas/sitemap/0.9";
        XDocument sitemap = XDocument.Parse(await _client.GetStringAsync("/sitemaps/genres.xml"));
        foreach (string loc in sitemap.Descendants(sm + "loc").Select(l => l.Value))
        {
            HttpResponseMessage response = await _client.GetAsync(new Uri(loc).PathAndQuery);
            IDocument doc = new HtmlParser().ParseDocument(await response.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(loc, doc.QuerySelector("link[rel=canonical]")!.GetAttribute("href"));
        }
    }
}
