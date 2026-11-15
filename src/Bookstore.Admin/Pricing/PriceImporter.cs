using System.Globalization;
using Bookstore.Bcl.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bookstore.Admin.Pricing;

public sealed class ImportOptions
{
    public decimal MaxPrice { get; set; } = 5000m;
}

public sealed record PriceRow(int Line, Isbn Isbn, decimal Price);

public sealed record ImportResult(int RowsRead, IReadOnlyList<PriceRow> ValidRows, IReadOnlyList<string> Errors, int Applied);

public sealed class PriceImporter(IPriceCatalog catalog, IOptions<ImportOptions> options, ILogger<PriceImporter> logger)
{
    public async Task<ImportResult> ImportAsync(string path, bool dryRun, int maxErrors, string reportPath, CancellationToken cancellationToken)
    {
        List<PriceRow> valid = [];
        List<string> errors = [];
        int line = 0;

        await foreach (string text in File.ReadLinesAsync(path, cancellationToken))
        {
            line++;
            if (line == 1 || string.IsNullOrWhiteSpace(text))
                continue;

            if (TryParseRow(line, text, out PriceRow? row, out string? error))
                valid.Add(row);
            else
                errors.Add(error);

            if (errors.Count >= maxErrors)
            {
                errors.Add($"stopped after {maxErrors} errors");
                break;
            }
        }

        int rowsRead = Math.Max(line - 1, 0);
        if (dryRun || errors.Count > 0)
            return new ImportResult(rowsRead, valid, errors, Applied: 0);

        int applied = await ApplyAsync(valid, reportPath, cancellationToken);
        return new ImportResult(rowsRead, valid, errors, applied);
    }

    private bool TryParseRow(int line, string text, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out PriceRow? row, [System.Diagnostics.CodeAnalysis.NotNullWhen(false)] out string? error)
    {
        row = null;
        string[] parts = text.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            error = $"line {line}: expected 2 columns (isbn,price), got {parts.Length}";
            return false;
        }
        if (!Isbn.TryParse(parts[0], out Isbn isbn))
        {
            error = $"line {line}: '{parts[0]}' is not a valid ISBN-13";
            return false;
        }
        if (!decimal.TryParse(parts[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal price)
            || price <= 0 || price > options.Value.MaxPrice)
        {
            error = $"line {line}: '{parts[1]}' is not a price between 0 and {options.Value.MaxPrice.ToString(CultureInfo.InvariantCulture)}";
            return false;
        }

        row = new PriceRow(line, isbn, price);
        error = null;
        return true;
    }

    private async Task<int> ApplyAsync(IReadOnlyList<PriceRow> rows, string reportPath, CancellationToken cancellationToken)
    {
        // The final name appears only when every row succeeded; anything else is kept as .partial.
        string tempPath = reportPath + ".tmp";
        int applied = 0;
        try
        {
            await using (StreamWriter report = new(tempPath))
            {
                await report.WriteLineAsync("isbn,old_price,new_price");
                foreach (PriceRow row in rows)
                {
                    decimal oldPrice = await catalog.UpdatePriceAsync(row.Isbn, row.Price, cancellationToken);
                    await report.WriteLineAsync(string.Create(CultureInfo.InvariantCulture, $"{row.Isbn},{oldPrice},{row.Price}"));
                    applied++;
                }
            }
            File.Move(tempPath, reportPath, overwrite: true);
            return applied;
        }
        catch when (File.Exists(tempPath))
        {
            string partialPath = reportPath + ".partial";
            File.Move(tempPath, partialPath, overwrite: true);
            logger.LogWarning("Import stopped: {Applied} of {Total} prices were changed, see {Partial}", applied, rows.Count, Path.GetFileName(partialPath));
            throw;
        }
    }
}
