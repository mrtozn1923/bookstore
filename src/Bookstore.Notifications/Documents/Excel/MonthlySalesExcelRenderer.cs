using ClosedXML.Excel;

namespace Bookstore.Notifications.Documents.Excel;

// Formatted back office report; ClosedXML keeps the whole workbook in memory (fine up to tens of thousands of rows)
public sealed class MonthlySalesExcelRenderer : IDocumentRenderer<MonthlySalesReport>
{
    private const string MoneyFormat = "#,##0.00 \"₺\"";

    public FileFormat Format => FileFormat.Xlsx;

    public Task RenderAsync(MonthlySalesReport model, Stream output, CancellationToken cancellationToken = default)
    {
        using XLWorkbook workbook = new();
        IXLWorksheet sales = workbook.Worksheets.Add("Sales");

        string[] headers = ["Order", "Placed at (UTC)", "Customer", "ISBN", "Title", "Quantity", "Unit price", "Total"];
        for (int c = 0; c < headers.Length; c++)
        {
            sales.Cell(1, c + 1).Value = headers[c];
        }

        int row = 2;
        foreach (SalesRow item in model.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            sales.Cell(row, 1).Value = item.OrderId;
            sales.Cell(row, 2).Value = item.PlacedAtUtc;               // a real date, not a string
            SetText(sales.Cell(row, 3), item.CustomerEmail);
            sales.Cell(row, 4).Value = item.Isbn;                      // text: 13 digits must not become 9.78044E+12
            SetText(sales.Cell(row, 5), item.Title);
            sales.Cell(row, 6).Value = item.Quantity;
            sales.Cell(row, 7).Value = item.UnitPrice;
            sales.Cell(row, 8).FormulaA1 = $"F{row}*G{row}";
            row++;
        }

        IXLTable table = sales.Range(1, 1, row - 1, headers.Length).CreateTable("SalesTable");
        table.ShowTotalsRow = true;
        table.Field("Quantity").TotalsRowFunction = XLTotalsRowFunction.Sum;
        table.Field("Total").TotalsRowFunction = XLTotalsRowFunction.Sum;

        sales.Column(2).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
        sales.Column(4).Style.NumberFormat.Format = "@";
        sales.Columns(7, 8).Style.NumberFormat.Format = MoneyFormat;
        sales.SheetView.FreezeRows(1);
        // Fixed widths: AdjustToContents measures text cell by cell and dominated the run time on large sheets
        double[] widths = [9, 17, 22, 15, 24, 9, 12, 13];
        for (int c = 0; c < widths.Length; c++)
        {
            sales.Column(c + 1).Width = widths[c];
        }

        AddSummary(workbook, model);
        workbook.SaveAs(output);
        return Task.CompletedTask;
    }

    private static void AddSummary(XLWorkbook workbook, MonthlySalesReport model)
    {
        IXLWorksheet summary = workbook.Worksheets.Add("Summary");
        summary.Cell("A1").Value = $"Monthly sales {model.Month:yyyy-MM}";
        summary.Cell("A1").Style.Font.SetBold().Font.SetFontSize(14);
        summary.Cell("A3").Value = "Title";
        summary.Cell("B3").Value = "Copies";
        summary.Cell("C3").Value = "Revenue";
        summary.Range("A3:C3").Style.Font.SetBold();

        int last = model.Rows.Count + 1;
        int row = 4;
        foreach (string title in model.Rows.Select(r => r.Title).Distinct().Order())
        {
            SetText(summary.Cell(row, 1), title);
            // Formulas over the Sales sheet: the numbers stay live if someone edits a row
            summary.Cell(row, 2).FormulaA1 = $"SUMIFS(Sales!$F$2:$F${last},Sales!$E$2:$E${last},A{row})";
            summary.Cell(row, 3).FormulaA1 = $"SUMIFS(Sales!$H$2:$H${last},Sales!$E$2:$E${last},A{row})";
            row++;
        }

        summary.Range(4, 3, row - 1, 3).Style.NumberFormat.Format = MoneyFormat;
        // Not AdjustToContents: measuring a formula cell evaluates it, and these formulas scan the whole Sales sheet
        summary.Column(1).Width = 24;
        summary.Columns(2, 3).Width = 14;
    }

    // Stored as text either way; quotePrefix also tells Excel to keep it text if a user edits the cell
    public static void SetText(IXLCell cell, string value)
    {
        cell.Value = value;
        if (value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
        {
            cell.Style.IncludeQuotePrefix = true;
        }
    }
}
