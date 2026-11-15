using Bookstore.Notifications.Documents;
using Bookstore.Notifications.Documents.Excel;
using Bookstore.Notifications.Samples;
using ClosedXML.Excel;

namespace Bookstore.Notifications.Tests;

public sealed class ExcelTests
{
    [Fact]
    public async Task Monthly_report_keeps_types_and_its_summary_adds_up()
    {
        MonthlySalesReport report = BookstoreSampleData.MonthlySales(new DateOnly(2026, 10, 1), rowCount: 500);
        using MemoryStream file = new();
        await new MonthlySalesExcelRenderer().RenderAsync(report, file, TestContext.Current.CancellationToken);

        file.Position = 0;
        using XLWorkbook workbook = new(file);
        IXLWorksheet sales = workbook.Worksheet("Sales");
        Assert.Equal(XLDataType.DateTime, sales.Cell("B2").DataType);
        Assert.Equal(XLDataType.Text, sales.Cell("D2").DataType);
        Assert.Equal(13, sales.Cell("D2").GetString().Length);

        IXLWorksheet summary = workbook.Worksheet("Summary");
        decimal revenue = summary.Column(3).CellsUsed().Skip(1).Sum(c => (decimal)c.Value.GetNumber());
        Assert.Equal(report.Rows.Sum(r => r.Total), revenue);
    }

    [Fact]
    public void Formula_like_text_stays_text_with_quote_prefix()
    {
        using XLWorkbook workbook = new();
        IXLCell cell = workbook.Worksheets.Add("S").Cell("A1");

        MonthlySalesExcelRenderer.SetText(cell, "=HYPERLINK(\"https://attacker.example.com\",\"Dune\")");

        Assert.False(cell.HasFormula);
        Assert.Equal(XLDataType.Text, cell.DataType);
        Assert.True(cell.Style.IncludeQuotePrefix);
    }

    [Fact]
    public void Filled_template_is_read_with_type_conversion_and_problems()
    {
        using MemoryStream template = new();
        InstitutionalOrderTemplate.Create(template, "Hilltop Primary School", ["Library"],
            [new CatalogEntry("9780547928227", "The Hobbit", 380m)]);

        template.Position = 0;
        using MemoryStream filled = new();
        using (XLWorkbook workbook = new(template))
        {
            IXLWorksheet order = workbook.Worksheet("Order");
            order.Cell("A6").Value = 9780547928227;      // pasted as a number
            order.Cell("C6").Value = "Library";
            order.Cell("D6").Value = 30;
            order.Cell("A7").Value = "9780547928227";
            order.Cell("D7").Value = "twenty";
            workbook.SaveAs(filled);
        }

        filled.Position = 0;
        InstitutionalOrderSheet sheet = InstitutionalOrderTemplate.Read(filled);

        InstitutionalOrderLine line = Assert.Single(sheet.Lines);
        Assert.Equal(("9780547928227", 30), (line.Isbn, line.Quantity));
        Assert.Contains("Row 7", Assert.Single(sheet.Problems));
    }
}
