using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace Bookstore.Notifications.Documents.Html;

public sealed class ChromiumPdfOptions
{
    [Range(1, 32)]
    public int MaxConcurrentPages { get; set; } = 4;

    public TimeSpan RenderTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

// One browser process for the whole application; every document gets its own short-lived context and page
public sealed class ChromiumPdfRenderer(
    RazorHtmlRenderer razor,
    IOptions<ChromiumPdfOptions> options,
    ILogger<ChromiumPdfRenderer> logger) : IDocumentRenderer<InstitutionalQuoteModel>, IAsyncDisposable
{
    private const string AssetHost = "https://assets.bookstore.invalid/";
    private static readonly string Css = LoadCss();
    private static readonly Dictionary<string, byte[]> Fonts = LoadFonts();
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private readonly SemaphoreSlim _pages = new(options.Value.MaxConcurrentPages, options.Value.MaxConcurrentPages);
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public FileFormat Format => FileFormat.Pdf;

    public async Task RenderAsync(InstitutionalQuoteModel model, Stream output, CancellationToken cancellationToken = default)
    {
        string html = await razor.RenderAsync<InstitutionalQuote>(new Dictionary<string, object?> { ["Model"] = model, ["Css"] = Css });
        byte[] pdf = await RenderHtmlAsync(html, $"Quote {model.QuoteNumber}", cancellationToken);
        await output.WriteAsync(pdf, cancellationToken);
    }

    public async Task<byte[]> RenderHtmlAsync(string html, string footerTitle, CancellationToken cancellationToken = default)
    {
        IBrowser browser = await GetBrowserAsync();
        await _pages.WaitAsync(cancellationToken);
        try
        {
            // JavaScript off and no network: the document can only use what is inside the HTML string
            await using IBrowserContext context = await browser.NewContextAsync(new() { JavaScriptEnabled = false });
            await context.RouteAsync("**/*", async route =>
            {
                // Allow-list: our own fonts, answered from memory; everything else is aborted
                if (route.Request.Url.StartsWith(AssetHost + "fonts/", StringComparison.Ordinal)
                    && Fonts.TryGetValue(route.Request.Url[(AssetHost.Length + 6)..], out byte[]? font))
                {
                    await route.FulfillAsync(new() { BodyBytes = font, ContentType = "font/ttf" });
                    return;
                }

                logger.LogWarning("Blocked request from document: {Method} {Url}", route.Request.Method, route.Request.Url);
                await route.AbortAsync();
            });

            IPage page = await context.NewPageAsync();
            page.SetDefaultTimeout((float)options.Value.RenderTimeout.TotalMilliseconds);
            await page.SetContentAsync(html);
            return await page.PdfAsync(new()
            {
                Format = "A4",
                PrintBackground = true,
                PreferCSSPageSize = true,
                DisplayHeaderFooter = true,
                HeaderTemplate = "<span></span>",
                FooterTemplate = $"""
                    <div style="font-family:sans-serif;font-size:8pt;color:#64748b;width:100%;padding:0 16mm;display:flex;justify-content:space-between">
                        <span>{System.Net.WebUtility.HtmlEncode(footerTitle)}</span>
                        <span>Page <span class="pageNumber"></span> / <span class="totalPages"></span></span>
                    </div>
                    """,
            });
        }
        finally
        {
            _pages.Release();
        }
    }

    private async Task<IBrowser> GetBrowserAsync()
    {
        if (_browser is { IsConnected: true })
        {
            return _browser;
        }

        await _startLock.WaitAsync();
        try
        {
            if (_browser is { IsConnected: true })
            {
                return _browser;
            }

            // First call, or the browser process died: start (again)
            _playwright ??= await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
            logger.LogInformation("Chromium {Version} started", _browser.Version);
            return _browser;
        }
        finally
        {
            _startLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
        }

        _playwright?.Dispose();
    }

    private static Dictionary<string, byte[]> LoadFonts()
    {
        const string prefix = "Bookstore.Notifications.fonts.";
        Dictionary<string, byte[]> fonts = [];
        foreach (string name in typeof(ChromiumPdfRenderer).Assembly.GetManifestResourceNames().Where(n => n.StartsWith(prefix, StringComparison.Ordinal)))
        {
            using Stream stream = typeof(ChromiumPdfRenderer).Assembly.GetManifestResourceStream(name)!;
            using MemoryStream buffer = new();
            stream.CopyTo(buffer);
            fonts[name[prefix.Length..]] = buffer.ToArray();
        }

        return fonts;
    }

    private static string LoadCss()
    {
        using Stream stream = typeof(ChromiumPdfRenderer).Assembly.GetManifestResourceStream("Bookstore.Notifications.quote.css")!;
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
