using CsvHelper;

namespace Bookstore.IO.Csv.Export;

// The accounting team's export: Turkish headers because it is opened in Turkish Excel.
[CsvRecord]
public sealed partial class OrderCsvRow
{
    [CsvColumn("Sipariş No")]
    public string OrderNumber { get; init; } = "";

    [CsvColumn("Müşteri")]
    public string Customer { get; init; } = "";

    [CsvColumn("Tarih"), CsvFormat("dd.MM.yyyy HH:mm:ss")]
    public DateTime CreatedAtUtc { get; init; }

    [CsvColumn("Durum")]
    public string Status { get; init; } = "";

    [CsvColumn("Tutar")]
    public decimal Total { get; init; }
}
