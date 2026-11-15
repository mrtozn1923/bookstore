using System.Text;
using Bookstore.Notifications.Documents.Html;
using Bookstore.Notifications.Documents.Pdf;
using Bookstore.Notifications.Samples;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace Bookstore.Notifications.Tests;

// Needs the Chromium build that matches Microsoft.Playwright (playwright.ps1 install chromium); skipped without it
public sealed class QuotePdfTests
{
    [Fact]
    public async Task Quote_pdf_contains_totals_and_repeats_the_table_header()
    {
        await using ServiceProvider services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<RazorHtmlRenderer>()
            .AddSingleton(Options.Create(new ChromiumPdfOptions()))
            .AddSingleton<ChromiumPdfRenderer>()
            .BuildServiceProvider();

        using MemoryStream pdf = new();
        try
        {
            await services.GetRequiredService<ChromiumPdfRenderer>()
                .RenderAsync(BookstoreSampleData.InstitutionalQuote(lineCount: 60), pdf, TestContext.Current.CancellationToken);
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("Executable doesn't exist"))
        {
            Assert.Skip("Chromium for this Playwright version is not installed");
        }

        // Chromium writes ligatures ("ﬁ" as one glyph); NFKC turns them back into letters
        string[] pages = [.. PdfFiles.PageTexts(pdf.ToArray()).Select(p => p.Normalize(NormalizationForm.FormKC))];

        Assert.Equal(3, pages.Length);
        Assert.All(pages, p => Assert.Contains("ISBN Title Qty Unit price Amount", p));
        Assert.Contains("Valid until 15 Aralık 2026", pages[0]);
        Assert.Contains("Prices are fixed", pages[^1]);
        Assert.Contains($"Page 3 / 3", pages[^1]);
    }
}
