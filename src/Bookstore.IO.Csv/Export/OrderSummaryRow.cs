using CsvHelper;
using Bookstore.Ordering;

namespace Bookstore.IO.Csv.Export;

// One row per order in invariant format; used by the benchmarks.
[CsvRecord]
public sealed partial class OrderSummaryRow
{
    public string OrderNumber { get; init; } = "";
    public string Customer { get; init; } = "";
    public DateTime CreatedAtUtc { get; init; }
    public string Status { get; init; } = "";
    public decimal Total { get; init; }

    public static byte[] CreateCsv(int count)
    {
        using var buffer = new MemoryStream();
        using (var csv = CsvWriter.Create(buffer, CsvWriterOptions.Default, leaveOpen: true))
        {
            csv.WriteRecords(SampleOrders.Generate(count).Select(o => new OrderSummaryRow
            {
                OrderNumber = o.OrderNumber,
                Customer = o.CustomerName,
                CreatedAtUtc = o.CreatedAtUtc,
                Status = o.Status.ToString(),
                Total = o.Total
            }));
        }
        return buffer.ToArray();
    }
}
