using ClosedXML.Excel;

namespace Bookstore.Notifications.Documents.Excel;

public sealed record CatalogEntry(string Isbn, string Title, decimal Price);

public sealed record InstitutionalOrderLine(int Row, string Isbn, string Unit, int Quantity);

public sealed record InstitutionalOrderSheet(IReadOnlyList<InstitutionalOrderLine> Lines, IReadOnlyList<string> Problems);

// A workbook the institution fills in and uploads back; only the input cells are editable
public static class InstitutionalOrderTemplate
{
    public const int FirstInputRow = 6;
    public const int InputRows = 200;

    public static void Create(Stream output, string institutionName, IReadOnlyList<string> units, IReadOnlyList<CatalogEntry> catalog)
    {
        using XLWorkbook workbook = new();
        IXLWorksheet order = workbook.Worksheets.Add("Order");
        IXLWorksheet lists = workbook.Worksheets.Add("Lists");

        for (int i = 0; i < units.Count; i++)
        {
            lists.Cell(i + 1, 1).Value = units[i];
        }

        for (int i = 0; i < catalog.Count; i++)
        {
            lists.Cell(i + 1, 3).Value = catalog[i].Isbn;
            lists.Cell(i + 1, 4).Value = catalog[i].Title;
            lists.Cell(i + 1, 5).Value = catalog[i].Price;
        }

        lists.Hide();

        order.Cell("A1").Value = "Bookstore institutional order";
        order.Cell("A1").Style.Font.SetBold().Font.SetFontSize(14);
        order.Cell("A2").Value = "Institution";
        order.Cell("B2").Value = institutionName;
        order.Cell("A3").Value = "Minimum 20 copies per order; 10% institutional discount is applied on our side.";

        string[] headers = ["ISBN", "Title", "Unit", "Quantity", "Unit price", "Line total"];
        for (int c = 0; c < headers.Length; c++)
        {
            order.Cell(FirstInputRow - 1, c + 1).Value = headers[c];
        }

        order.Range(FirstInputRow - 1, 1, FirstInputRow - 1, headers.Length).Style.Font.SetBold();
        int last = FirstInputRow + InputRows - 1;

        IXLRange isbn = order.Range(FirstInputRow, 1, last, 1);
        isbn.Style.NumberFormat.Format = "@";
        isbn.CreateDataValidation().TextLength.EqualTo(13);

        IXLRange unit = order.Range(FirstInputRow, 3, last, 3);
        unit.CreateDataValidation().List(lists.Range(1, 1, units.Count, 1));

        IXLRange quantity = order.Range(FirstInputRow, 4, last, 4);
        quantity.CreateDataValidation().WholeNumber.Between(1, 10_000);

        for (int r = FirstInputRow; r <= last; r++)
        {
            // Looked up from the hidden list: the customer cannot change titles or prices
            order.Cell(r, 2).FormulaA1 = $"IF(A{r}=\"\",\"\",IFERROR(VLOOKUP(A{r},Lists!$C:$E,2,FALSE),\"Unknown ISBN\"))";
            order.Cell(r, 5).FormulaA1 = $"IF(A{r}=\"\",\"\",IFERROR(VLOOKUP(A{r},Lists!$C:$E,3,FALSE),\"\"))";
            order.Cell(r, 6).FormulaA1 = $"IF(OR(D{r}=\"\",E{r}=\"\"),\"\",D{r}*E{r})";
        }

        order.Range(FirstInputRow, 5, last, 6).Style.NumberFormat.Format = "#,##0.00 \"₺\"";
        order.Range(FirstInputRow, 1, last, 1).Style.Protection.SetLocked(false);
        order.Range(FirstInputRow, 3, last, 4).Style.Protection.SetLocked(false);
        order.Protect().AllowElement(XLSheetProtectionElements.FormatColumns);
        order.Columns().AdjustToContents();
        order.Column(2).Width = 32;

        workbook.SaveAs(output);
    }

    // Excel reading at API level only: types and per-cell problems, no import pipeline
    public static InstitutionalOrderSheet Read(Stream input)
    {
        using XLWorkbook workbook = new(input);
        IXLWorksheet order = workbook.Worksheet("Order");
        List<InstitutionalOrderLine> lines = [];
        List<string> problems = [];

        foreach (IXLRow row in order.Rows(FirstInputRow, FirstInputRow + InputRows - 1))
        {
            IXLCell isbnCell = row.Cell(1);
            if (isbnCell.IsEmpty())
            {
                continue;
            }

            // Typed as a number in a General cell, an ISBN arrives as a double
            string isbn = isbnCell.Value.Type switch
            {
                XLDataType.Number => isbnCell.Value.GetNumber().ToString("0", System.Globalization.CultureInfo.InvariantCulture),
                _ => isbnCell.GetString().Trim(),
            };

            if (!row.Cell(4).TryGetValue(out int quantity) || quantity < 1)
            {
                problems.Add($"Row {row.RowNumber()}: quantity '{row.Cell(4).GetString()}' is not a whole number");
                continue;
            }

            lines.Add(new InstitutionalOrderLine(row.RowNumber(), isbn, row.Cell(3).GetString(), quantity));
        }

        return new InstitutionalOrderSheet(lines, problems);
    }
}
