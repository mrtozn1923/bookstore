using System.Globalization;
using System.Text;

namespace Bookstore.Notifications.Documents;

// Temporary renderers for post 01; QuestPDF (05) and ClosedXML (04) replace them
public sealed class PlainTextInvoiceRenderer : IDocumentRenderer<InvoiceModel>
{
    public FileFormat Format => FileFormat.PlainText;

    public async Task RenderAsync(InvoiceModel model, Stream output, CancellationToken cancellationToken = default)
    {
        CultureInfo tr = CultureInfo.GetCultureInfo("tr-TR");
        await using StreamWriter writer = new(output, new UTF8Encoding(false), leaveOpen: true);
        await writer.WriteLineAsync($"Invoice {model.InvoiceNumber} / order {model.OrderId}");
        foreach (InvoiceLine line in model.Lines)
        {
            await writer.WriteLineAsync($"{line.Quantity} x {line.Title,-14} {line.NetAmount.ToString("C", tr),12}");
        }

        await writer.WriteLineAsync($"Total incl. VAT: {model.GrandTotal.ToString("C", tr)}");
    }
}

public sealed class PlainTextSalesReportRenderer(TimeSpan simulatedRenderTime) : IDocumentRenderer<MonthlySalesReport>
{
    public FileFormat Format => FileFormat.PlainText;

    public async Task RenderAsync(MonthlySalesReport model, Stream output, CancellationToken cancellationToken = default)
    {
        // Stands in for the real Excel rendering cost until post 04
        await Task.Delay(simulatedRenderTime, cancellationToken);

        await using StreamWriter writer = new(output, new UTF8Encoding(false), leaveOpen: true);
        await writer.WriteLineAsync($"Monthly sales {model.Month:yyyy-MM}: {model.Rows.Count} rows, {model.Rows.Sum(r => r.Total):0.00} TRY");
        foreach (SalesRow row in model.Rows)
        {
            await writer.WriteLineAsync($"{row.OrderId};{row.PlacedAtUtc:O};{row.Isbn};{row.Quantity};{row.Total:0.00}");
        }
    }
}
