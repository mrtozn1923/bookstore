using System.IO.Compression;
using System.Xml.Linq;
using Bookstore.IO.Xml.Invoices;
using Bookstore.Ordering;

namespace Bookstore.IO.Compression;

public static class InvoiceZip
{
    public static readonly DateOnly IssueDate = new(2026, 11, 14);

    // Her fatura bir entry; ZipArchive hedef stream'e sırayla yazar, seek gerekmez.
    public static async Task WriteAsync(Stream destination, IEnumerable<Order> orders, CancellationToken ct = default)
    {
        await using ZipArchive archive = await ZipArchive.CreateAsync(destination, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding: null, ct);
        foreach (Order order in orders)
        {
            ZipArchiveEntry entry = archive.CreateEntry($"invoices/{order.OrderNumber}.xml", CompressionLevel.Optimal);
            await using Stream entryStream = await entry.OpenAsync(ct);
            await InvoiceXml.Create(order, IssueDate).SaveAsync(entryStream, SaveOptions.None, ct);
        }
    }
}
