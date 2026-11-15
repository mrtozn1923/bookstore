using System.Xml.Linq;
using AngleSharp.Dom;

namespace Bookstore.SeoAudit;

public enum Severity { Error, Warning }

public sealed record Finding(Severity Severity, string Rule, string Page, string Detail);

public sealed class Auditor(Fetcher fetcher)
{
    private static readonly XNamespace Sm = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private readonly List<Finding> _findings = [];
    private readonly HashSet<string> _reachable = [];
    private readonly HashSet<string> _checkedLinks = [];

    public IReadOnlyList<Finding> Findings => _findings;
    public int SitemapUrls { get; private set; }
    public int CrawledPages => _reachable.Count;

    public async Task RunAsync()
    {
        await fetcher.LoadRobotsAsync();
        List<string> sitemap = await ReadSitemapAsync("/sitemap.xml");
        SitemapUrls = sitemap.Count;

        // 1) link'lerden keşif: ana sayfalardan BFS, robots.txt'nin engellediği URL'lere girmeden
        Queue<string> queue = new(["/", "/en/"]);
        while (queue.TryDequeue(out string? path))
        {
            if (!_reachable.Add(path))
                continue;
            Page page = await fetcher.GetAsync(path);
            if (page.Html is null)
                continue;
            CheckPage(page);
            foreach (string next in await CheckLinksAsync(page))
                queue.Enqueue(next);
            await CheckHreflangAsync(page);
        }

        // 2) sitemap: her URL 200, index'lenebilir, canonical'ı kendisi ve link'le ulaşılabilir olmalı
        foreach (string path in sitemap)
        {
            Page page = await fetcher.GetAsync(path);
            if (page.Status != 200)
            {
                Add(Severity.Error, "sitemap-status", path, $"listed in sitemap but returns {page.Status}");
                continue;
            }
            if (IsNoindex(page))
                Add(Severity.Error, "sitemap-noindex", path, "listed in sitemap but has noindex");
            string? canonical = Canonical(page);
            if (canonical != fetcher.ToSiteUrl(path))
                Add(Severity.Error, "sitemap-canonical", path, $"canonical is {canonical ?? "missing"}");
            if (!_reachable.Contains(path))
                Add(Severity.Warning, "orphan", path, "in sitemap but no internal link reaches it");
        }

        await CheckDuplicateTitlesAsync();
    }

    private void CheckPage(Page page)
    {
        IDocument html = page.Html!;
        if (page.Status == 200 && Canonical(page) is null)
            Add(Severity.Error, "canonical-missing", page.Path, "no rel=canonical");
        if (page.Status == 200 && string.IsNullOrWhiteSpace(html.QuerySelector("meta[name=description]")?.GetAttribute("content")))
            Add(Severity.Warning, "description-missing", page.Path, "empty or missing meta description");
        foreach (IElement img in html.QuerySelectorAll("img"))
        {
            if (string.IsNullOrWhiteSpace(img.GetAttribute("src")))
                Add(Severity.Error, "img-src", page.Path, $"img without src: {Trim(img.OuterHtml)}");
            else if (!img.HasAttribute("alt"))
                Add(Severity.Warning, "img-alt", page.Path, $"img without alt: {img.GetAttribute("src")}");
        }
    }

    // sayfadaki iç link'leri izler; kırık link hata, redirect'ten geçen link uyarı
    private async Task<List<string>> CheckLinksAsync(Page page)
    {
        List<string> next = [];
        foreach (IElement a in page.Html!.QuerySelectorAll("a[href]"))
        {
            string? path = fetcher.ToPath(a.GetAttribute("href")!, page.Path);
            if (path is null || fetcher.IsDisallowed(path))
                continue;
            (List<string> hops, int status) = await fetcher.ResolveAsync(path);
            bool first = _checkedLinks.Add($"{page.Path} {path}");
            if (first && status is >= 400 or 0)
                Add(Severity.Error, "broken-link", page.Path, $"{path} -> {(status == 0 ? "redirect loop" : status)}");
            else if (first && hops.Count > 1)
                Add(Severity.Warning, "redirect-link", page.Path, $"{string.Join(" -> ", hops)}");
            if (status == 200)
                next.Add(hops[^1]);
        }
        return next;
    }

    // hreflang: her alternate 200 dönmeli ve bu sayfaya geri link vermeli
    private async Task CheckHreflangAsync(Page page)
    {
        string? self = Canonical(page);
        if (page.Status != 200 || self != fetcher.ToSiteUrl(page.Path))
            return; // yalnız canonical sayfalar hreflang taşır
        foreach (IElement link in page.Html!.QuerySelectorAll("link[rel=alternate][hreflang]"))
        {
            string href = link.GetAttribute("href")!;
            if (href == self)
                continue;
            Page alternate = await fetcher.GetAsync(fetcher.ToPath(href)!);
            if (alternate.Status != 200)
                Add(Severity.Error, "hreflang-target", page.Path, $"{link.GetAttribute("hreflang")} -> {href} returns {alternate.Status}");
            else if (alternate.Html?.QuerySelectorAll("link[rel=alternate][hreflang]").All(l => l.GetAttribute("href") != self) != false)
                Add(Severity.Error, "hreflang-return", page.Path, $"{href} has no hreflang link back");
        }
    }

    private async Task CheckDuplicateTitlesAsync()
    {
        Dictionary<string, List<string>> byTitle = [];
        foreach (string path in _reachable.Order())
        {
            // sayfalı listeler (?page=n) aynı title'ı taşıyabilir; Google bunu sorun saymıyor
            Page page = await fetcher.GetAsync(path);
            if (page.Status != 200 || page.Html is null || path.Contains("page=") || Canonical(page) != fetcher.ToSiteUrl(path))
                continue;
            // TR ve EN sürümleri hreflang ile bağlı; aynı title'ı taşımaları tekrar sayılmaz: dil başına grupla
            string key = $"{page.Html.DocumentElement.GetAttribute("lang")}|{page.Html.Title}";
            byTitle[key] = [.. byTitle.GetValueOrDefault(key, []), path];
        }
        foreach ((string key, List<string> paths) in byTitle.Where(x => x.Value.Count > 1))
            Add(Severity.Warning, "duplicate-title", paths[0], $"\"{key.Split('|')[1]}\" also on {string.Join(", ", paths.Skip(1))}");
    }

    // sitemap index'i ve alt sitemap'leri okur; loc'lar production URL'i, path'e çevrilir
    private async Task<List<string>> ReadSitemapAsync(string path)
    {
        XDocument doc = XDocument.Parse(await fetcher.GetTextAsync(path));
        List<string> urls = [];
        foreach (string loc in doc.Descendants(Sm + "loc").Select(l => l.Value.Trim()))
        {
            string p = fetcher.ToPath(loc)!;
            urls.AddRange(doc.Root!.Name.LocalName == "sitemapindex" ? await ReadSitemapAsync(p) : [p]);
        }
        return urls;
    }

    private static bool IsNoindex(Page page) =>
        (page.XRobotsTag ?? "").Contains("noindex") ||
        (page.Html?.QuerySelector("meta[name=robots]")?.GetAttribute("content") ?? "").Contains("noindex");

    private static string? Canonical(Page page) => page.Html?.QuerySelector("link[rel=canonical]")?.GetAttribute("href");

    private static string Trim(string s) => s.Length > 80 ? s[..80] + "…" : s;

    private void Add(Severity severity, string rule, string page, string detail) => _findings.Add(new(severity, rule, page, detail));
}
