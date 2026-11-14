using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Bookstore.Ordering;

namespace Bookstore.IO.Xml.Invoices;

public sealed record InvoiceLineInfo(string Isbn, string Title, decimal Quantity, decimal LineTotal, string Currency);

public static class InvoiceXml
{
    private static string Amount(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    // LINQ to XML: build the whole document in memory. Fine for a single invoice.
    public static XDocument Create(Order order, DateOnly issueDate)
    {
        XNamespace cac = Ubl.Cac, cbc = Ubl.Cbc;
        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(Ubl.Invoice + "Invoice",
                new XAttribute(XNamespace.Xmlns + "cac", cac),
                new XAttribute(XNamespace.Xmlns + "cbc", cbc),
                new XElement(cbc + "UBLVersionID", "2.1"),
                new XElement(cbc + "ID", order.OrderNumber.Replace("BS-", "BSI2026")),
                new XElement(cbc + "IssueDate", issueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                new XElement(cbc + "DocumentCurrencyCode", order.Currency),
                new XElement(cac + "AccountingCustomerParty",
                    new XElement(cac + "Party",
                        new XElement(cac + "PartyName",
                            new XElement(cbc + "Name", order.CustomerName)))),
                order.Items.Select((item, index) =>
                    new XElement(cac + "InvoiceLine",
                        new XElement(cbc + "ID", index + 1),
                        new XElement(cbc + "InvoicedQuantity", new XAttribute("unitCode", "C62"), item.Quantity),
                        new XElement(cbc + "LineExtensionAmount", new XAttribute("currencyID", order.Currency), Amount(item.LineTotal)),
                        new XElement(cac + "Item",
                            new XElement(cbc + "Name", item.Title),
                            new XElement(cac + "StandardItemIdentification",
                                new XElement(cbc + "ID", item.Isbn))),
                        new XElement(cac + "Price",
                            new XElement(cbc + "PriceAmount", new XAttribute("currencyID", order.Currency), Amount(item.UnitPrice))))),
                new XElement(cac + "LegalMonetaryTotal",
                    new XElement(cbc + "PayableAmount", new XAttribute("currencyID", order.Currency), Amount(order.Total)))));
    }

    public static IEnumerable<InvoiceLineInfo> ReadLines(XDocument invoice)
    {
        XNamespace cac = Ubl.Cac, cbc = Ubl.Cbc;
        return invoice.Root!
            .Elements(cac + "InvoiceLine")
            .Select(line => new InvoiceLineInfo(
                Isbn: (string)line.Element(cac + "Item")!.Element(cac + "StandardItemIdentification")!.Element(cbc + "ID")!,
                Title: (string)line.Element(cac + "Item")!.Element(cbc + "Name")!,
                Quantity: (decimal)line.Element(cbc + "InvoicedQuantity")!,
                LineTotal: (decimal)line.Element(cbc + "LineExtensionAmount")!,
                Currency: (string)line.Element(cbc + "LineExtensionAmount")!.Attribute("currencyID")!));
    }

    public static XmlSchemaSet LoadSchemas()
    {
        var schemas = new XmlSchemaSet();
        string dir = Path.Join(AppContext.BaseDirectory, "Schemas");
        schemas.Add(Ubl.Cbc.NamespaceName, Path.Join(dir, "cbc.xsd"));
        schemas.Add(Ubl.Cac.NamespaceName, Path.Join(dir, "cac.xsd"));
        schemas.Add(Ubl.Invoice.NamespaceName, Path.Join(dir, "invoice.xsd"));
        schemas.Compile();
        return schemas;
    }

    // Validates while reading; collects every error instead of stopping at the first one.
    public static List<string> Validate(TextReader xml, XmlSchemaSet schemas)
    {
        var errors = new List<string>();
        var settings = new XmlReaderSettings
        {
            ValidationType = ValidationType.Schema,
            Schemas = schemas,
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };
        settings.ValidationEventHandler += (_, e) =>
            errors.Add($"{e.Severity} line {e.Exception.LineNumber}: {e.Message}");

        using var reader = XmlReader.Create(xml, settings);
        while (reader.Read()) { }
        return errors;
    }
}
