using System.Text.Encodings.Web;
using System.Text.Unicode;
using Bookstore.Web.Catalog;
using Bookstore.Web.Seo;
using Bookstore.Web.Videos;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<SiteOptions>(builder.Configuration.GetSection("Site"));
builder.Services.AddSingleton<BookCatalog>();
builder.Services.AddSingleton<VideoCatalog>();
// Türkçe karakterleri (ı, ğ, ş) HTML entity'ye çevirmeden yaz: alt ve title kaynakta da okunur kalsın
builder.Services.Configure<WebEncoderOptions>(o => o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));
builder.Services.Configure<RouteOptions>(o => o.LowercaseUrls = true);
builder.Services.AddRazorPages(o =>
{
    o.Conventions.AddPageRoute("/Books/Detail", "en/books/{slug}");
    o.Conventions.AddPageRoute("/Videos/Watch", "en/videos/{slug}");
    o.Conventions.AddPageRoute("/Videos/Index", "en/videos");
    o.Conventions.AddPageRoute("/Books/Genre", "en/books/genre/{genre}/{language?}");
    o.Conventions.AddPageRoute("/Index", "en");
});

WebApplication app = builder.Build();

app.UseStatusCodePagesWithReExecute("/not-found");
app.MapStaticAssets();
app.MapRazorPages();

// filtre ve sıralama URL'leri crawl edilmez; tür ve sayfa URL'leri serbest
app.MapGet("/robots.txt", () => Results.Text("""
    User-agent: *
    Disallow: /*?*format=
    Disallow: /*?*sort=

    Sitemap: https://bookstore.example/sitemap.xml

    """, "text/plain; charset=utf-8"));

// Angular istemcisinin katalog API'si: sayfayla aynı kurallar, sonuçsuz ya da geçersiz istekte 404
app.MapGet("/api/catalog/{genre}/{language?}", (string genre, string? language, HttpRequest request, BookCatalog catalog) =>
{
    (CatalogQuery? query, _) = CatalogQuery.Parse(genre, language, request.Query);
    if (query is null)
        return Results.NotFound();
    List<Book> all = [.. query.Apply(catalog.Books)];
    int totalPages = (int)Math.Ceiling(all.Count / (double)CatalogQuery.PageSize);
    if (all.Count == 0 || query.Page > totalPages)
        return Results.NotFound();
    var items = all.Skip((query.Page - 1) * CatalogQuery.PageSize).Take(CatalogQuery.PageSize)
        .Select(b => new { b.Slug, b.Title, b.Author });
    return Results.Ok(new { items, query.Page, totalPages });
});

app.MapGet("/sitemap.xml", async (HttpContext ctx, IOptions<SiteOptions> site) =>
{
    ctx.Response.ContentType = "application/xml; charset=utf-8";
    await SitemapWriter.WriteIndexAsync(ctx.Response.Body, site.Value, ["books", "genres", "videos"], ctx.RequestAborted);
});

app.MapGet("/sitemaps/genres.xml", async (HttpContext ctx, BookCatalog catalog, IOptions<SiteOptions> site) =>
{
    ctx.Response.ContentType = "application/xml; charset=utf-8";
    await SitemapWriter.WriteGenresAsync(ctx.Response.Body, catalog, site.Value, ctx.RequestAborted);
});

app.MapGet("/sitemaps/books.xml", async (HttpContext ctx, BookCatalog catalog, IOptions<SiteOptions> site) =>
{
    ctx.Response.ContentType = "application/xml; charset=utf-8";
    await SitemapWriter.WriteBooksAsync(ctx.Response.Body, catalog.Books, site.Value, ctx.RequestAborted);
});

app.MapGet("/sitemaps/videos.xml", async (HttpContext ctx, VideoCatalog videos, IOptions<SiteOptions> site) =>
{
    ctx.Response.ContentType = "application/xml; charset=utf-8";
    await VideoSitemapWriter.WriteAsync(ctx.Response.Body, videos.Videos, site.Value, ctx.RequestAborted);
});

app.Run();

public partial class Program;
