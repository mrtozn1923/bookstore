using System.Globalization;
using System.Text;
using Bookstore.Ordering;
using CsvHelper;

namespace Bookstore.IO.Csv.Bank;

public sealed record ImportResult(int Imported, decimal Total, IReadOnlyList<string> Errors);

public static class BankFile
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    // Simulates the file the bank sends: Windows-1254, ';' separated, Turkish number and date formats.
    public static void WriteSample(string path, int count)
    {
        using var writer = new StreamWriter(path, append: false, Encoding.GetEncoding(1254)) { NewLine = "\r\n" };
        writer.WriteLine("İşlem Tarihi;Açıklama;Tutar;Referans");
        int i = 0;
        foreach (var order in SampleOrders.Generate(count))
        {
            i++;
            string date = order.CreatedAtUtc.AddDays(1).ToString("dd.MM.yyyy", Turkish);
            string amount = order.Total.ToString("N2", Turkish);
            string description = $"{order.OrderNumber} sipariş ödemesi - {order.CustomerName.ToUpper(Turkish)}";
            string line = i switch
            {
                250 => $"{date};\"{description}; kısmi\";{amount};TRX{i:D8}",
                400 => $"{date};{description};1.2O4,50;TRX{i:D8}",
                600 => $"31.11.2026;{description};{amount};TRX{i:D8}",
                800 => $"{date};{description}",
                _ => $"{date};{description};{amount};TRX{i:D8}"
            };
            writer.WriteLine(line);
        }
    }

    public static ImportResult Import(string path)
    {
        var errors = new List<string>();
        var options = new CsvReaderOptions
        {
            Separator = ';',
            Encoding = Encoding.GetEncoding(1254),
            FormatProvider = Turkish,
            TrackLineNumbers = true,
            // Hatalı satırı raporla ve atla; import durmasın.
            OnRecordError = error =>
            {
                long line = (error.Exception as CsvFormatException)?.LineNumber ?? 0;
                errors.Add($"line {line}: {error.Exception.Message} | {error.RawRecord}");
                return true;
            }
        };

        int imported = 0;
        decimal total = 0;
        using var csv = CsvReader.FromFile(path, options);
        foreach (BankPayment payment in csv.GetRecords<BankPayment>())
        {
            imported++;
            total += payment.Amount;
        }
        return new ImportResult(imported, total, errors);
    }
}
