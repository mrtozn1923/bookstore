using CsvHelper;

namespace Bookstore.IO.Csv.Bank;

// The bank's collection file: Turkish headers, dd.MM.yyyy dates, amounts like 1.234,50.
[CsvRecord]
public sealed partial class BankPayment
{
    [CsvColumn("İşlem Tarihi"), CsvFormat("dd.MM.yyyy")]
    public DateOnly Date { get; init; }

    [CsvColumn("Açıklama")]
    public string Description { get; init; } = "";

    [CsvColumn("Tutar")]
    public decimal Amount { get; init; }

    [CsvColumn("Referans")]
    public string Reference { get; init; } = "";
}
