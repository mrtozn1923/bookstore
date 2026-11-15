using System.Text.Json;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Bookstore.Web.Catalog;
using Bookstore.Web.Images;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Bookstore.Web.Tests;

public sealed class BookPageImageTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private async Task<IDocument> GetAsync(string path)
    {
        HttpClient client = factory.CreateClient();
        string html = await client.GetStringAsync(path);
        return new HtmlParser().ParseDocument(html);
    }

    [Theory]
    [InlineData("/books/dune", "Dune, Frank Herbert, ciltli baskı kapağı")]
    [InlineData("/en/books/dune", "Cover of Dune by Frank Herbert, hardcover edition")]
    public async Task Main_cover_has_localized_alt_and_is_not_lazy(string path, string expectedAlt)
    {
        IDocument doc = await GetAsync(path);
        IElement cover = doc.QuerySelector("picture img")!;

        Assert.Equal(expectedAlt, cover.GetAttribute("alt"));
        Assert.Null(cover.GetAttribute("loading"));
        Assert.EndsWith("dune-frank-herbert-640.jpg", cover.GetAttribute("src"));
    }

    [Fact]
    public async Task Every_img_has_a_real_src_and_an_alt_attribute()
    {
        IDocument doc = await GetAsync("/books/dune");
        foreach (IElement img in doc.QuerySelectorAll("img"))
        {
            Assert.False(string.IsNullOrWhiteSpace(img.GetAttribute("src")), img.OuterHtml);
            Assert.True(img.HasAttribute("alt"), img.OuterHtml); // dekoratif görselde alt="" olur ama attribute vardır
            Assert.NotNull(img.GetAttribute("width"));
            Assert.NotNull(img.GetAttribute("height"));
        }
    }

    [Fact]
    public async Task Og_image_and_primary_image_point_to_the_same_large_cover()
    {
        IDocument doc = await GetAsync("/books/dune");
        string og = doc.QuerySelector("meta[property='og:image']")!.GetAttribute("content")!;
        using JsonDocument ld = JsonDocument.Parse(doc.QuerySelector("script[type='application/ld+json']")!.TextContent);

        Assert.Equal(og, ld.RootElement.GetProperty("primaryImageOfPage").GetString());
        Assert.EndsWith("-1200.jpg", og);
        Assert.Contains("max-image-preview:large", doc.QuerySelector("meta[name='robots']")!.GetAttribute("content"));
    }

    [Fact]
    public void Every_referenced_cover_file_exists()
    {
        string root = Path.Combine(AppContext.BaseDirectory, "../../../../../src/Bookstore.Web/wwwroot");
        string[] missing = [.. new BookCatalog().Books
            .SelectMany(b => CoverImages.Widths.SelectMany(w => new[] { "avif", "webp", "jpg" }.Select(ext => CoverImages.Url(b, w, ext, ""))))
            .Where(url => !File.Exists(Path.Combine(root, url.TrimStart('/'))))];
        Assert.Empty(missing);
    }
}
