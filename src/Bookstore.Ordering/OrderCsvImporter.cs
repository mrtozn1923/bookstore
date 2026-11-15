using System.Globalization;
using Bookstore.Catalog;

namespace Bookstore.Ordering;

public sealed record ImportedLine(int CustomerId, Isbn Isbn, int Quantity, decimal UnitPrice);

public sealed record ImportResult(IReadOnlyList<ImportedLine> Lines, IReadOnlyList<string> Errors);

public sealed record CsvRow(int Number, string[] Fields);

public class OrderCsvImporter
{
    public ImportResult Import(string csv) => OrderRowValidator.Validate(OrderCsvParser.Parse(csv));
}

public static class OrderCsvParser
{
    public static IReadOnlyList<CsvRow> Parse(string csv)
    {
        List<CsvRow> rows = [];
        int row = 0;
        foreach (string raw in csv.Split('\n'))
        {
            row++;
            if (row == 1 || raw.Trim() == "")
                continue;
            rows.Add(new CsvRow(row, raw.Trim().Split(';')));
        }
        return rows;
    }
}

public static class OrderRowValidator
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    public static ImportResult Validate(IEnumerable<CsvRow> rows)
    {
        List<ImportedLine> lines = [];
        List<string> errors = [];
        foreach (CsvRow csvRow in rows)
        {
            int row = csvRow.Number;
            string[] f = csvRow.Fields;
            if (f.Length != 4)
            {
                errors.Add($"Row {row}: expected 4 fields, found {f.Length}.");
                continue;
            }
            if (!int.TryParse(f[0], out int customerId))
            {
                errors.Add($"Row {row}: customer id '{f[0]}' is not a number.");
                continue;
            }
            if (!Isbn.TryParse(f[1], out Isbn? isbn))
            {
                errors.Add($"Row {row}: '{f[1]}' is not a valid ISBN.");
                continue;
            }
            if (!int.TryParse(f[2], out int quantity) || quantity < 1 || quantity > OrderSettings.MaxQuantityPerLine)
            {
                errors.Add($"Row {row}: quantity '{f[2]}' must be between 1 and {OrderSettings.MaxQuantityPerLine}.");
                continue;
            }
            if (!decimal.TryParse(f[3], NumberStyles.Number, Turkish, out decimal price))
            {
                errors.Add($"Row {row}: price '{f[3]}' is not a number.");
                continue;
            }
            lines.Add(new ImportedLine(customerId, isbn, quantity, price));
        }
        return new ImportResult(lines, errors);
    }
}
