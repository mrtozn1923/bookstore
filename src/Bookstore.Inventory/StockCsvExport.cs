using System.Text;

namespace Bookstore.Inventory;

// Nightly stock file for the old accounting system, which reads Windows-1254 (Turkish) text
public static class StockCsvExport
{
    public static byte[] ToLegacyCsv(IEnumerable<StockItem> items)
    {
        // .NET (Core and later) ships only a few encodings; code pages come from this provider
        Encoding turkish = CodePagesEncodingProvider.Instance.GetEncoding(1254)!;
        StringBuilder csv = new("isbn;warehouse;available\n");
        foreach (StockItem item in items)
        {
            csv.Append($"{item.Isbn};{item.Warehouse};{item.Available}\n");
        }

        return turkish.GetBytes(csv.ToString());
    }
}
