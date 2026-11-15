using System.Text;
using System.Xml;
using Bookstore.Web.Catalog;
using Bookstore.Web.Images;

namespace Bookstore.Web.Seo;

public static class SitemapWriter
{
    private const string Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private const string Xhtml = "http://www.w3.org/1999/xhtml";
    private const string ImageNs = "http://www.google.com/schemas/sitemap-image/1.1";

    // async yazma, dosya başında BOM yok (Teknik SEO 04)
    private static readonly XmlWriterSettings Settings = new() { Async = true, Encoding = new UTF8Encoding(false) };

    public static async Task WriteBooksAsync(Stream output, IEnumerable<Book> books, SiteOptions site, CancellationToken ct)
    {
        await using XmlWriter xml = XmlWriter.Create(output, Settings);
        await xml.WriteStartDocumentAsync();
        await xml.WriteStartElementAsync(null, "urlset", Ns);
        await xml.WriteAttributeStringAsync("xmlns", "xhtml", null, Xhtml);
        await xml.WriteAttributeStringAsync("xmlns", "image", null, ImageNs);

        foreach (Book book in books)
        {
            ct.ThrowIfCancellationRequested();
            string tr = $"{site.BaseUrl}/books/{book.Slug}";
            string en = $"{site.BaseUrl}/en/books/{book.Slug}";
            // kapak iki dilde de aynı URL: Google aynı görseli tek kaynak olarak görür
            string cover = CoverImages.Url(book, 640, "jpg", site.AbsoluteImageBase);

            foreach (string loc in (string[])[tr, en])
            {
                await xml.WriteStartElementAsync(null, "url", Ns);
                await xml.WriteElementStringAsync(null, "loc", Ns, loc);
                await xml.WriteElementStringAsync(null, "lastmod", Ns, book.ContentUpdatedAt.ToString("yyyy-MM-dd"));
                await WriteAlternateAsync(xml, "tr", tr);
                await WriteAlternateAsync(xml, "en", en);
                await WriteAlternateAsync(xml, "x-default", tr);
                await WriteImageAsync(xml, cover);
                await xml.WriteEndElementAsync();
            }
        }
        await xml.WriteEndElementAsync();
    }

    // image sitemap: yalnız image:image ve image:loc; caption, title, license 2022'de kaldırıldı
    private static async Task WriteImageAsync(XmlWriter xml, string imageUrl)
    {
        await xml.WriteStartElementAsync("image", "image", ImageNs);
        await xml.WriteElementStringAsync("image", "loc", ImageNs, imageUrl);
        await xml.WriteEndElementAsync();
    }

    private static async Task WriteAlternateAsync(XmlWriter xml, string hreflang, string href)
    {
        await xml.WriteStartElementAsync("xhtml", "link", Xhtml);
        await xml.WriteAttributeStringAsync(null, "rel", null, "alternate");
        await xml.WriteAttributeStringAsync(null, "hreflang", null, hreflang);
        await xml.WriteAttributeStringAsync(null, "href", null, href);
        await xml.WriteEndElementAsync();
    }
}
