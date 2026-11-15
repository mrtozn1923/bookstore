using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace Bookstore.SeoAudit;

public sealed record Page(string Path, int Status, string? Location, IDocument? Html, string? XRobotsTag);

// Test edilen uygulamayı (ör. http://127.0.0.1:52213) production adresiyle (https://bookstore.example) eşler:
// HTML'deki canonical, hreflang ve sitemap URL'leri production'daki gibi kalır, istekler yerel uygulamaya gider.
public sealed class Fetcher(Uri target, Uri site)
{
    private readonly HttpClient _http = new(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(10) };
    private readonly Dictionary<string, Page> _cache = [];
    private readonly HtmlParser _parser = new();
    private List<Regex> _disallow = [];

    public int Requests { get; private set; }

    // site'ın kendi URL'i mi? Öyleyse path + query, değilse null (dış link)
    public string? ToPath(string url, string basePath = "/")
    {
        Uri resolved = new(new Uri(site, basePath), url);
        bool own = resolved.Authority == site.Authority || resolved.Authority == target.Authority;
        return own && resolved.Scheme.StartsWith("http") ? resolved.PathAndQuery : null;
    }

    public string ToSiteUrl(string path) => new Uri(site, path).ToString();

    public async Task<Page> GetAsync(string path)
    {
        if (_cache.TryGetValue(path, out Page? cached))
            return cached;
        Requests++;
        using HttpResponseMessage r = await _http.GetAsync(new Uri(target, path));
        IDocument? html = r.Content.Headers.ContentType?.MediaType == "text/html"
            ? await _parser.ParseDocumentAsync(await r.Content.ReadAsStringAsync()) : null;
        string? xRobots = r.Headers.TryGetValues("X-Robots-Tag", out IEnumerable<string>? v) ? string.Join(", ", v) : null;
        return _cache[path] = new Page(path, (int)r.StatusCode, r.Headers.Location?.OriginalString, html, xRobots);
    }

    public Task<string> GetTextAsync(string path) => _http.GetStringAsync(new Uri(target, path));

    // redirect zincirini izler: [ilk path, ..., son path] ve son status
    public async Task<(List<string> Hops, int Status)> ResolveAsync(string path)
    {
        List<string> hops = [path];
        for (int i = 0; i < 5; i++)
        {
            Page page = await GetAsync(hops[^1]);
            if (page.Status is < 300 or > 399 || page.Location is null)
                return (hops, page.Status);
            hops.Add(ToPath(page.Location, hops[^1])!);
        }
        return (hops, 0); // 5 hop'tan uzun zincir ya da döngü
    }

    // robots.txt: yalnız "User-agent: *" grubunun Disallow satırları (RFC 9309 wildcard'ları)
    public async Task LoadRobotsAsync()
    {
        string text = await _http.GetStringAsync(new Uri(target, "/robots.txt"));
        _disallow = [.. text.Split('\n').Select(l => l.Trim())
            .Where(l => l.StartsWith("Disallow:", StringComparison.OrdinalIgnoreCase) && l.Length > 9)
            .Select(l => new Regex("^" + Regex.Escape(l[9..].Trim()).Replace(@"\*", ".*").Replace(@"\$", "$")))];
    }

    public bool IsDisallowed(string path) => _disallow.Any(r => r.IsMatch(path));
}
