using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Bookstore.Notifications.Documents.Excel;

// Streams rows with the Open XML SDK's SAX-style OpenXmlWriter: memory stays flat whatever the row count
public static class LargeSalesExportWriter
{
    private const uint DateStyle = 1;
    private const uint MoneyStyle = 2;

    public static void Write(IEnumerable<SalesRow> rows, Stream output)
    {
        using SpreadsheetDocument document = SpreadsheetDocument.Create(output, SpreadsheetDocumentType.Workbook);
        WorkbookPart workbookPart = document.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();
        AddStyles(workbookPart);

        WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        using (OpenXmlWriter writer = OpenXmlWriter.Create(worksheetPart))
        {
            writer.WriteStartElement(new Worksheet());
            writer.WriteStartElement(new SheetData());

            WriteRow(writer, ["Order", "Placed at (UTC)", "Customer", "ISBN", "Title", "Quantity", "Unit price", "Total"]);
            foreach (SalesRow row in rows)
            {
                writer.WriteStartElement(new Row());
                WriteNumber(writer, row.OrderId);
                WriteNumber(writer, row.PlacedAtUtc.ToOADate(), DateStyle);
                WriteText(writer, row.CustomerEmail);
                WriteText(writer, row.Isbn);
                WriteText(writer, row.Title);
                WriteNumber(writer, row.Quantity);
                WriteNumber(writer, (double)row.UnitPrice, MoneyStyle);
                WriteNumber(writer, (double)row.Total, MoneyStyle);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        workbookPart.Workbook.AppendChild(new Sheets(new Sheet
        {
            Id = workbookPart.GetIdOfPart(worksheetPart),
            SheetId = 1,
            Name = "Sales",
        }));
        workbookPart.Workbook.Save();
    }

    private static void WriteRow(OpenXmlWriter writer, string[] values)
    {
        writer.WriteStartElement(new Row());
        foreach (string value in values)
        {
            WriteText(writer, value);
        }

        writer.WriteEndElement();
    }

    // Inline strings: no shared string table to keep in memory
    private static void WriteText(OpenXmlWriter writer, string value) =>
        writer.WriteElement(new Cell(new InlineString(new Text(value))) { DataType = CellValues.InlineString });

    private static void WriteNumber(OpenXmlWriter writer, double value, uint style = 0) =>
        writer.WriteElement(new Cell(new CellValue(value.ToString(CultureInfo.InvariantCulture))) { StyleIndex = style });

    private static void AddStyles(WorkbookPart workbookPart)
    {
        WorkbookStylesPart styles = workbookPart.AddNewPart<WorkbookStylesPart>();
        styles.Stylesheet = new Stylesheet(
            new NumberingFormats(
                new NumberingFormat { NumberFormatId = 164, FormatCode = "yyyy-mm-dd hh:mm" },
                new NumberingFormat { NumberFormatId = 165, FormatCode = "#,##0.00 \"₺\"" }),
            new Fonts(new Font()),
            new Fills(new Fill(new PatternFill { PatternType = PatternValues.None }), new Fill(new PatternFill { PatternType = PatternValues.Gray125 })),
            new Borders(new Border()),
            new CellFormats(
                new CellFormat(),
                new CellFormat { NumberFormatId = 164, ApplyNumberFormat = true },
                new CellFormat { NumberFormatId = 165, ApplyNumberFormat = true }));
    }
}
