using System.Text.Encodings.Web;
using System.Text.Unicode;
using Bookstore.Web.Catalog;
using Bookstore.Web.Seo;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<SiteOptions>(builder.Configuration.GetSection("Site"));
builder.Services.AddSingleton<BookCatalog>();
// Türkçe karakterleri (ı, ğ, ş) HTML entity'ye çevirmeden yaz: alt ve title kaynakta da okunur kalsın
builder.Services.Configure<WebEncoderOptions>(o => o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));
builder.Services.Configure<RouteOptions>(o => o.LowercaseUrls = true);
builder.Services.AddRazorPages(o => o.Conventions.AddPageRoute("/Books/Detail", "en/books/{slug}"));

WebApplication app = builder.Build();

app.UseStatusCodePagesWithReExecute("/not-found");
app.MapStaticAssets();
app.MapRazorPages();

app.MapGet("/sitemaps/books.xml", async (HttpContext ctx, BookCatalog catalog, IOptions<SiteOptions> site) =>
{
    ctx.Response.ContentType = "application/xml; charset=utf-8";
    await SitemapWriter.WriteBooksAsync(ctx.Response.Body, catalog.Books, site.Value, ctx.RequestAborted);
});

app.Run();

public partial class Program;
