using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Bookstore.SeoAudit.Tests;

// bilinen hatalar içeren küçük bir site: her kural en az bir kez tetiklenmeli
public sealed class AuditorTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private Uri _target = null!;
    private static readonly Uri Site = new("https://shop.example/");

    private static IResult Html(string lang, string title, string head, string body = "") => Results.Content(
        $"<!DOCTYPE html><html lang=\"{lang}\"><head><title>{title}</title><meta name=\"description\" content=\"{title}\">{head}</head><body>{body}</body></html>",
        "text/html; charset=utf-8");

    private static string Canonical(string path) => $"<link rel=\"canonical\" href=\"https://shop.example{path}\">";

    public async Task InitializeAsync()
    {
        _app = WebApplication.CreateBuilder().Build();
        _app.Urls.Add("http://127.0.0.1:0");
        _app.MapGet("/robots.txt", () => "User-agent: *\nDisallow: /search\n");
        _app.MapGet("/sitemap.xml", () => Results.Content("""
            <urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
              <url><loc>https://shop.example/a</loc></url><url><loc>https://shop.example/b</loc></url>
              <url><loc>https://shop.example/c</loc></url><url><loc>https://shop.example/old</loc></url>
            </urlset>
            """, "application/xml"));
        _app.MapGet("/", () => Html("en", "Home", Canonical("/"),
            "<a href=\"/a\">A</a><a href=\"/old\">old</a><a href=\"/missing\">missing</a><a href=\"/search?q=x\">search</a><a href=\"/c\">C</a>"));
        _app.MapGet("/en/", () => Results.Redirect("/", permanent: true));
        _app.MapGet("/a", () => Html("en", "A", Canonical("/a") +
            "<link rel=\"alternate\" hreflang=\"en\" href=\"https://shop.example/a\"><link rel=\"alternate\" hreflang=\"tr\" href=\"https://shop.example/tr/a\">",
            "<img src=\"/a.png\">"));
        _app.MapGet("/tr/a", () => Html("tr", "A (tr)", Canonical("/tr/a") +
            "<link rel=\"alternate\" hreflang=\"tr\" href=\"https://shop.example/tr/a\">"));
        _app.MapGet("/b", () => Html("en", "B", Canonical("/b") + "<meta name=\"robots\" content=\"noindex\">"));
        _app.MapGet("/c", () => Html("en", "A", Canonical("/a")));
        _app.MapGet("/old", () => Results.Redirect("/a", permanent: true));
        await _app.StartAsync();
        _target = new Uri(_app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First());
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task Finds_every_seeded_problem()
    {
        Auditor auditor = new(new Fetcher(_target, Site));
        await auditor.RunAsync();
        string[] found = [.. auditor.Findings.Select(f => $"{f.Rule} {f.Page}").Order()];

        Assert.Equal(
        [
            "broken-link /",              // /missing -> 404
            "hreflang-return /a",         // /tr/a, /a'ya geri link vermiyor
            "img-alt /a",
            "orphan /b",                  // sitemap'te var, link yok
            "redirect-link /",            // /old -> /a
            "sitemap-canonical /c",       // canonical başka sayfayı gösteriyor
            "sitemap-noindex /b",
            "sitemap-status /old",        // sitemap'te 301
        ], found);
    }
}
