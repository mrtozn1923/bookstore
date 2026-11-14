using System.Globalization;
using Bookstore.Ordering;
using CsvHelper;

namespace Bookstore.IO.Csv.Export;

public static class OrderCsvExport
{
    // Excel'in Türkçe bölge ayarlarıyla doğru açacağı biçim.
    public static readonly CsvWriterOptions ExcelTurkish = new()
    {
        Separator = ';',
        FormatProvider = CultureInfo.GetCultureInfo("tr-TR"),
        WriteByteOrderMark = true,
        Injection = CsvInjection.Escape
    };

    public static void Write(string path, IEnumerable<Order> orders, CsvWriterOptions options)
    {
        using var csv = CsvWriter.ToFile(path, options);
        csv.WriteRecords(orders.Select(o => new OrderCsvRow
        {
            OrderNumber = o.OrderNumber,
            Customer = o.CustomerName,
            CreatedAtUtc = o.CreatedAtUtc,
            Status = o.Status.ToString(),
            Total = o.Total
        }));
    }
}
