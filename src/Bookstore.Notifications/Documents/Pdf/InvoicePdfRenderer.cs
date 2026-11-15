using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Bookstore.Notifications.Documents.Pdf;

public sealed class InvoicePdfRenderer : IDocumentRenderer<InvoiceModel>
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly byte[] Logo = LoadLogo();

    public FileFormat Format => FileFormat.Pdf;

    public async Task RenderAsync(InvoiceModel model, Stream output, CancellationToken cancellationToken = default)
    {
        // QuestPDF writes synchronously: render into memory, then copy asynchronously (Kestrel forbids sync I/O)
        using MemoryStream buffer = new();
        Document.Create(container => Compose(container, model)).GeneratePdf(buffer);
        buffer.Position = 0;
        await buffer.CopyToAsync(output, cancellationToken);
    }

    public static IDocument CreateDocument(InvoiceModel model) => Document.Create(container => Compose(container, model));

    private static void Compose(IDocumentContainer container, InvoiceModel model)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(36);
            page.DefaultTextStyle(text => text.FontSize(10).FontColor(Colors.Grey.Darken4));

            page.Header().Element(header => ComposeHeader(header, model));
            page.Content().PaddingVertical(16).Element(content => ComposeContent(content, model));
            page.Footer().AlignCenter().Text(text =>
            {
                text.Span("Page ");
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        });
    }

    private static void ComposeHeader(IContainer container, InvoiceModel model)
    {
        container.Row(row =>
        {
            row.ConstantItem(150).Height(40).Image(Logo).FitArea();
            row.RelativeItem().AlignRight().Column(column =>
            {
                column.Item().Text($"Invoice {model.InvoiceNumber}").FontSize(16).Bold();
                column.Item().Text($"Order #{model.OrderId}");
                column.Item().Text($"Issue date: {model.IssueDate.ToString("d MMMM yyyy", Tr)}");
            });
        });
    }

    private static void ComposeContent(IContainer container, InvoiceModel model)
    {
        container.Column(column =>
        {
            column.Spacing(14);
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(seller =>
                {
                    seller.Item().Text("Seller").Bold();
                    seller.Item().Text("Bookstore");
                    seller.Item().Text("orders@example.com");
                });
                row.RelativeItem().Column(buyer =>
                {
                    PostalAddress a = model.BillingAddress;
                    buyer.Item().Text("Bill to").Bold();
                    buyer.Item().Text(a.Name);
                    buyer.Item().Text(a.Line1);
                    buyer.Item().Text($"{a.PostalCode} {a.City}, {a.Country}");
                    buyer.Item().Text(model.CustomerEmail);
                });
            });

            column.Item().Element(table => ComposeLines(table, model));
            column.Item().AlignRight().Width(260).Element(summary => ComposeVatSummary(summary, model));
        });
    }

    private static void ComposeLines(IContainer container, InvoiceModel model)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(95);
                columns.RelativeColumn();
                columns.ConstantColumn(40);
                columns.ConstantColumn(70);
                columns.ConstantColumn(40);
                columns.ConstantColumn(75);
            });

            // Repeated at the top of every page the table continues on
            table.Header(header =>
            {
                foreach (string title in new[] { "ISBN", "Title", "Qty", "Unit price", "VAT", "Net amount" })
                {
                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingVertical(4).Text(title).Bold();
                }
            });

            foreach (InvoiceLine line in model.Lines)
            {
                table.Cell().Element(Cell).Text(line.Isbn);
                table.Cell().Element(Cell).Text(line.Title);
                table.Cell().Element(Cell).AlignRight().Text(line.Quantity.ToString(Tr));
                table.Cell().Element(Cell).AlignRight().Text(line.UnitPrice.ToString("C", Tr));
                table.Cell().Element(Cell).AlignRight().Text(line.VatRate.ToString("P0", Tr));
                table.Cell().Element(Cell).AlignRight().Text(line.NetAmount.ToString("C", Tr));
            }
        });

        static IContainer Cell(IContainer cell) => cell.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3);
    }

    private static void ComposeVatSummary(IContainer container, InvoiceModel model)
    {
        container.Column(column =>
        {
            foreach (IGrouping<decimal, InvoiceLine> rate in model.Lines.GroupBy(l => l.VatRate).OrderBy(g => g.Key))
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text($"VAT {rate.Key.ToString("P0", Tr)} on {rate.Sum(l => l.NetAmount).ToString("C", Tr)}");
                    row.ConstantItem(80).AlignRight().Text(rate.Sum(l => l.VatAmount).ToString("C", Tr));
                });
            }

            column.Item().PaddingTop(4).BorderTop(1).Row(row =>
            {
                row.RelativeItem().Text("Net total");
                row.ConstantItem(80).AlignRight().Text(model.NetTotal.ToString("C", Tr));
            });
            column.Item().Row(row =>
            {
                row.RelativeItem().Text("VAT total");
                row.ConstantItem(80).AlignRight().Text(model.VatTotal.ToString("C", Tr));
            });
            column.Item().Row(row =>
            {
                row.RelativeItem().Text("Grand total").Bold();
                row.ConstantItem(80).AlignRight().Text(model.GrandTotal.ToString("C", Tr)).Bold();
            });
        });
    }

    private static byte[] LoadLogo()
    {
        using Stream stream = typeof(InvoicePdfRenderer).Assembly.GetManifestResourceStream("Bookstore.Notifications.logo.png")!;
        using MemoryStream copy = new();
        stream.CopyTo(copy);
        return copy.ToArray();
    }
}
