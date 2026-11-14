using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Bookstore.Ordering;

namespace Bookstore.IO.Xml.Invoices;

// A monthly batch: <InvoiceBatch> with one UBL-like <Invoice> per order.
public static class InvoiceBatch
{
    public static async Task WriteAsync(Stream destination, IEnumerable<Order> orders, DateOnly issueDate)
    {
        var settings = new XmlWriterSettings { Async = true, Indent = false, Encoding = new System.Text.UTF8Encoding(false) };
        await using XmlWriter writer = XmlWriter.Create(destination, settings);
        await writer.WriteStartDocumentAsync();
        await writer.WriteStartElementAsync(null, "InvoiceBatch", "urn:bookstore:invoices:batch");
        await writer.WriteAttributeStringAsync("xmlns", "cac", null, Ubl.Cac.NamespaceName);
        await writer.WriteAttributeStringAsync("xmlns", "cbc", null, Ubl.Cbc.NamespaceName);
        foreach (var order in orders)
        {
            // Each invoice is built as a small XElement and written straight away.
            XElement invoice = InvoiceXml.Create(order, issueDate).Root!;
            invoice.Attributes().Where(a => a.IsNamespaceDeclaration && a.Name.Namespace == XNamespace.Xmlns).Remove(); // cac/cbc are declared once on the batch root
            await invoice.WriteToAsync(writer, CancellationToken.None);
        }
        await writer.WriteEndElementAsync();
        await writer.WriteEndDocumentAsync();
    }

    // Hybrid read: XmlReader walks the file, each <Invoice> becomes a short-lived XElement.
    public static async Task<(int Invoices, decimal Total)> SumStreamingAsync(Stream source)
    {
        var settings = new XmlReaderSettings { Async = true, DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
        using XmlReader reader = XmlReader.Create(source, settings);
        XName payable = Ubl.Cbc + "PayableAmount";
        int count = 0;
        decimal total = 0;

        await reader.MoveToContentAsync();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "Invoice" && reader.NamespaceURI == Ubl.Invoice.NamespaceName)
            {
                // ReadFrom moves the reader past the element, so no extra Read() here.
                var invoice = (XElement)await XNode.ReadFromAsync(reader, CancellationToken.None);
                total += (decimal)invoice.Descendants(payable).Single();
                count++;
            }
            else
            {
                await reader.ReadAsync();
            }
        }
        return (count, total);
    }

    public static (int Invoices, decimal Total) SumWithXDocument(Stream source)
    {
        XDocument doc = XDocument.Load(source);
        var amounts = doc.Descendants(Ubl.Cbc + "PayableAmount").Select(a => (decimal)a).ToList();
        return (amounts.Count, amounts.Sum());
    }
}
