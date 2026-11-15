using System.Diagnostics;
using Bookstore.Notifications.Documents.Html;
using Bookstore.Notifications.Documents.Pdf;
using Bookstore.Notifications.Samples;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;
string command = args.FirstOrDefault() ?? "quote";
Directory.CreateDirectory("out");

ServiceCollection services = new();
services.AddLogging(b => b.AddSimpleConsole(o => o.SingleLine = true).SetMinimumLevel(LogLevel.Information)
    .AddFilter("Microsoft", LogLevel.Warning));
services.AddSingleton<RazorHtmlRenderer>();
services.AddSingleton(Options.Create(new ChromiumPdfOptions()));
services.AddSingleton<ChromiumPdfRenderer>();
await using ServiceProvider provider = services.BuildServiceProvider();
ChromiumPdfRenderer renderer = provider.GetRequiredService<ChromiumPdfRenderer>();

switch (command)
{
    case "quote":
    {
        int lines = args.Length > 1 ? int.Parse(args[1]) : 4;
        Stopwatch watch = Stopwatch.StartNew();
        await using (FileStream file = File.Create("out/quote.pdf"))
        {
            await renderer.RenderAsync(BookstoreSampleData.InstitutionalQuote(lines), file);
        }

        Console.WriteLine($"out/quote.pdf {new FileInfo("out/quote.pdf").Length / 1024} KB in {watch.ElapsedMilliseconds} ms");
        IReadOnlyList<string> pages = PdfFiles.PageTexts(await File.ReadAllBytesAsync("out/quote.pdf"));
        for (int i = 0; i < pages.Count; i++)
        {
            string text = pages[i];
            int rows = text.Split(' ').Count(w => w.Length == 13 && w.StartsWith("978", StringComparison.Ordinal));
            if (pages.Count == 1)
            {
                Console.WriteLine(text);
            }

            Console.WriteLine($"page {i + 1}: header row={text.Contains("ISBN Title Qty")} rows={rows} total={text.Contains("Total ₺")} footer='{System.Text.RegularExpressions.Regex.Match(text, @"Page \d+ / \d+").Value}'");
        }

        using (UglyToad.PdfPig.PdfDocument pdf = UglyToad.PdfPig.PdfDocument.Open("out/quote.pdf"))
        {
            Console.WriteLine("fonts: " + string.Join(", ", pdf.GetPages().SelectMany(p => p.Letters).Select(l => l.FontName).Distinct()));
        }

        break;
    }
    case "html":
    {
        // Same HTML the PDF is printed from, for the designer to open in a browser
        string html = await provider.GetRequiredService<RazorHtmlRenderer>().RenderAsync<InstitutionalQuote>(
            new Dictionary<string, object?> { ["Model"] = BookstoreSampleData.InstitutionalQuote(), ["Css"] = await File.ReadAllTextAsync(args[1]) });
        await File.WriteAllTextAsync("out/quote.html", html);
        Console.WriteLine($"out/quote.html {html.Length} chars");
        break;
    }
    case "lifetime":
    {
        int count = int.Parse(args[1]);
        string html = "<html><body><h1>Quote Q-2026-0117</h1></body></html>";

        Stopwatch watch = Stopwatch.StartNew();
        for (int i = 0; i < count; i++)
        {
            // Wrong: a new browser process for every document
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync();
            IPage page = await browser.NewPageAsync();
            await page.SetContentAsync(html);
            await page.PdfAsync();
        }

        Console.WriteLine($"new browser per document: {count} PDFs in {watch.ElapsedMilliseconds} ms ({watch.ElapsedMilliseconds / count} ms each)");

        await renderer.RenderHtmlAsync(html, "warm-up");
        watch.Restart();
        for (int i = 0; i < count; i++)
        {
            await renderer.RenderHtmlAsync(html, "Quote");
        }

        Console.WriteLine($"shared browser, new context: {count} PDFs in {watch.ElapsedMilliseconds} ms ({watch.ElapsedMilliseconds / count} ms each)");

        watch.Restart();
        await Task.WhenAll(Enumerable.Range(0, count).Select(_ => renderer.RenderHtmlAsync(html, "Quote")));
        Console.WriteLine($"shared browser, {count} in parallel (max 4 pages): {watch.ElapsedMilliseconds} ms");
        break;
    }
    case "untrusted":
    {
        // Stands in for an internal service that is reachable from the server but not from the internet
        using System.Net.HttpListener internalService = new();
        internalService.Prefixes.Add("http://127.0.0.1:52566/");
        internalService.Start();
        _ = Task.Run(async () =>
        {
            while (internalService.IsListening)
            {
                System.Net.HttpListenerContext request = await internalService.GetContextAsync();
                Console.WriteLine($"internal service got: {request.Request.HttpMethod} {request.Request.Url}");
                byte[] body = "<p>internal export token: fake-api-key-for-tests</p>"u8.ToArray();
                request.Response.ContentType = "text/html";
                await request.Response.OutputStream.WriteAsync(body);
                request.Response.Close();
            }
        });

        // A sales note that reached the template as raw HTML (e.g. via MarkupString)
        string note = """
            <iframe src="http://127.0.0.1:52566/admin/export" width="500" height="40"></iframe>
            <iframe src="secret.txt" width="500" height="40"></iframe>
            <script>document.body.append('script ran')</script>
            """;
        string html = $"<html><body><h1>Quote Q-2026-0117</h1><div>{note}</div><p>end of note</p></body></html>";
        await File.WriteAllTextAsync("out/secret.txt", "contents of secret.txt");

        byte[] safe = await renderer.RenderHtmlAsync(html, "Quote");
        Console.WriteLine($"renderer: {string.Join(" | ", PdfFiles.PageTexts(safe))}");

        // The common shortcut: save the HTML as a file and open it with file://, JavaScript on, no routing
        await File.WriteAllTextAsync("out/quote-untrusted.html", html);
        using IPlaywright playwright = await Playwright.CreateAsync();
        await using IBrowser browser = await playwright.Chromium.LaunchAsync();
        IPage page = await browser.NewPageAsync();
        await page.GotoAsync(new Uri(Path.GetFullPath("out/quote-untrusted.html")).AbsoluteUri);
        Console.WriteLine($"file:// page: {string.Join(" | ", PdfFiles.PageTexts(await page.PdfAsync()))}");
        break;
    }
    case "merge":
    {
        byte[] quote = await File.ReadAllBytesAsync("out/quote.pdf");
        // Stands in for the legal team's terms PDF, which we receive as a finished file
        byte[] terms = Document.Create(c => c.Page(p =>
        {
            p.Margin(50);
            p.Content().Column(col =>
            {
                col.Item().Text("Terms and Conditions for Institutional Orders").FontSize(16).Bold();
                col.Item().PaddingTop(10).Text("1. Orders of 20 or more copies receive a 10% institutional discount.");
                col.Item().Text("2. Partial shipments are invoiced per shipment.");
            });
        })).GeneratePdf();

        Stopwatch watch = Stopwatch.StartNew();
        byte[] merged = PdfFiles.Merge(quote, terms);
        await File.WriteAllBytesAsync("out/quote-package.pdf", merged);
        IReadOnlyList<string> pages = PdfFiles.PageTexts(merged);
        Console.WriteLine($"quote={PdfFiles.PageTexts(quote).Count} page(s) + terms={PdfFiles.PageTexts(terms).Count} page(s) -> {pages.Count} pages, {merged.Length / 1024} KB in {watch.ElapsedMilliseconds} ms");
        for (int i = 0; i < pages.Count; i++)
        {
            Console.WriteLine($"page {i + 1}: {pages[i][..Math.Min(pages[i].Length, 70)]}");
        }

        break;
    }
}
