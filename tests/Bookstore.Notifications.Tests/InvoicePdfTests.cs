using Bookstore.Notifications.Documents;
using Bookstore.Notifications.Documents.Pdf;
using Bookstore.Notifications.Samples;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Bookstore.Notifications.Tests;

public sealed class InvoicePdfTests
{
    public InvoicePdfTests() => QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

    private static async Task<PdfDocument> RenderAsync(InvoiceModel invoice)
    {
        MemoryStream pdf = new();
        await new InvoicePdfRenderer().RenderAsync(invoice, pdf, TestContext.Current.CancellationToken);
        return PdfDocument.Open(pdf.ToArray());
    }

    [Fact]
    public async Task Turkish_characters_and_totals_survive_in_the_pdf_text()
    {
        using PdfDocument pdf = await RenderAsync(BookstoreSampleData.Invoice(1042));
        string text = string.Join(" ", pdf.GetPage(1).GetWords().Select(w => w.Text));

        Assert.Contains("Bağdat Caddesi No: 12, Kadıköy", text);
        Assert.Contains("34710 İstanbul, Türkiye", text);
        Assert.Contains("Grand total ₺1.702,00", text);
    }

    [Fact]
    public async Task Long_invoice_repeats_the_table_header_and_numbers_pages()
    {
        using PdfDocument pdf = await RenderAsync(BookstoreSampleData.LongInvoice(1043, lineCount: 120));

        Assert.True(pdf.NumberOfPages > 1);
        foreach (Page page in pdf.GetPages())
        {
            string text = string.Join(" ", page.GetWords().Select(w => w.Text));
            Assert.Contains("ISBN Title Qty Unit price VAT Net amount", text);
            Assert.Contains($"Page {page.Number} / {pdf.NumberOfPages}", text);
        }
    }
}
