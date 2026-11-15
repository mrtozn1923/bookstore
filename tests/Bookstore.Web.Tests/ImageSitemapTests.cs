using System.Xml.Linq;
using Bookstore.Web.Catalog;
using Bookstore.Web.Seo;

namespace Bookstore.Web.Tests;

public sealed class ImageSitemapTests
{
    private static readonly XNamespace Sm = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private static readonly XNamespace Img = "http://www.google.com/schemas/sitemap-image/1.1";

    private static async Task<XDocument> WriteAsync()
    {
        SiteOptions site = new() { BaseUrl = "https://bookstore.example", ImageBaseUrl = "https://cdn.bookstore.example" };
        using MemoryStream stream = new();
        await SitemapWriter.WriteBooksAsync(stream, new BookCatalog().Books, site, CancellationToken.None);
        stream.Position = 0;
        return XDocument.Load(stream);
    }

    [Fact]
    public async Task Every_book_url_lists_its_cover_on_the_cdn()
    {
        XDocument doc = await WriteAsync();
        List<XElement> urls = [.. doc.Root!.Elements(Sm + "url")];

        Assert.Equal(new BookCatalog().Books.Count * 2, urls.Count); // TR + EN
        foreach (XElement url in urls)
        {
            XElement image = Assert.Single(url.Elements(Img + "image"));
            string loc = image.Element(Img + "loc")!.Value;
            Assert.StartsWith("https://cdn.bookstore.example/images/covers/", loc);
            Assert.EndsWith("-640.jpg", loc);
        }
    }

    [Fact]
    public async Task Tr_and_en_entries_share_the_same_image_url()
    {
        XDocument doc = await WriteAsync();
        string Image(string loc) => doc.Root!.Elements(Sm + "url")
            .Single(u => u.Element(Sm + "loc")!.Value == loc)
            .Element(Img + "image")!.Element(Img + "loc")!.Value;

        Assert.Equal(Image("https://bookstore.example/books/dune"), Image("https://bookstore.example/en/books/dune"));
    }

    [Fact]
    public async Task Only_supported_image_tags_are_written()
    {
        XDocument doc = await WriteAsync();
        string[] names = [.. doc.Descendants().Where(e => e.Name.Namespace == Img).Select(e => e.Name.LocalName).Distinct().Order()];
        Assert.Equal(["image", "loc"], names); // caption, title, geo_location, license 2022'de kaldırıldı
    }
}
