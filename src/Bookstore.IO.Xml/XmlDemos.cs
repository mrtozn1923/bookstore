using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Bookstore.IO.Xml.Invoices;
using Bookstore.IO.Xml.Shipping;
using Bookstore.Ordering;

namespace Bookstore.IO.Xml;

public static class XmlDemos
{
    private static readonly Order SampleOrder = SampleOrders.Generate(3).Last();
    private static readonly DateOnly IssueDate = new(2026, 11, 14);

    public static void Write()
    {
        XDocument invoice = InvoiceXml.Create(SampleOrder, IssueDate);
        Directory.CreateDirectory("out");
        invoice.Save(Path.Join("out", "invoice.xml"));
        Console.WriteLine(File.ReadAllText(Path.Join("out", "invoice.xml")));
    }

    public static void Read()
    {
        XDocument invoice = InvoiceXml.Create(SampleOrder, IssueDate);
        Console.WriteLine($"Without namespace: {invoice.Root!.Elements("InvoiceLine").Count()} lines");
        Console.WriteLine($"With namespace:    {invoice.Root!.Elements(Ubl.Cac + "InvoiceLine").Count()} lines");
        foreach (var line in InvoiceXml.ReadLines(invoice))
        {
            Console.WriteLine($"  {line.Isbn} {line.Title,-22} x{line.Quantity} = {line.LineTotal:0.00} {line.Currency}");
        }
    }

    public static void Validate()
    {
        var schemas = InvoiceXml.LoadSchemas();
        string valid = InvoiceXml.Create(SampleOrder, IssueDate).ToString();
        Console.WriteLine($"Valid invoice: {InvoiceXml.Validate(new StringReader(valid), schemas).Count} errors");

        XNamespace cbc = Ubl.Cbc;
        XDocument missingId = XDocument.Parse(valid);
        missingId.Root!.Element(cbc + "ID")!.Remove();
        Report("Missing ID", InvoiceXml.Validate(new StringReader(missingId.ToString()), schemas));

        XDocument badValues = XDocument.Parse(valid);
        badValues.Root!.Descendants(cbc + "InvoicedQuantity").First().Value = "two";
        badValues.Root!.Descendants(cbc + "PayableAmount").Single().Attribute("currencyID")!.Remove();
        Report("Bad values", InvoiceXml.Validate(new StringReader(badValues.ToString()), schemas));

        static void Report(string label, List<string> errors)
        {
            Console.WriteLine($"{label}: {errors.Count} errors");
            foreach (var e in errors) Console.WriteLine($"  {e}");
        }
    }

    public static async Task BatchAsync(string mode, int count)
    {
        Directory.CreateDirectory("out");
        string path = Path.Join("out", $"invoices-{count}.xml");
        if (!File.Exists(path) || mode == "write")
        {
            var sw0 = Stopwatch.StartNew();
            await using var output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            await InvoiceBatch.WriteAsync(output, SampleOrders.Generate(count), IssueDate);
            Console.WriteLine($"write   {count:N0} invoices in {sw0.ElapsedMilliseconds:N0} ms, peak heap {PeakHeap():N0} bytes");
            if (mode == "write") return;
        }

        var sw = Stopwatch.StartNew();
        await using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        var (invoices, total) = mode == "xdocument"
            ? InvoiceBatch.SumWithXDocument(input)
            : await InvoiceBatch.SumStreamingAsync(input);
        Console.WriteLine($"{mode,-9} {invoices:N0} invoices, {total:N2} TRY, file {new FileInfo(path).Length:N0} bytes, " +
                          $"{sw.ElapsedMilliseconds:N0} ms, peak heap {PeakHeap():N0} bytes");
    }

    private static long PeakHeap()
    {
        long peak = 0;
        foreach (GCKind kind in new[] { GCKind.Ephemeral, GCKind.FullBlocking, GCKind.Background })
        {
            peak = Math.Max(peak, GC.GetGCMemoryInfo(kind).HeapSizeBytes);
        }
        return Math.Max(peak, GC.GetTotalMemory(false));
    }

    private static ShipmentNotice SampleNotice() => new()
    {
        OrderNumber = "BS-0000042",
        Carrier = "ACME Cargo",
        TrackingCode = "TRK-123456",
        Parcels = [new Parcel { WeightKg = 0.8m, Isbn = "9780441172719" }, new Parcel { WeightKg = 0.5m, Isbn = "9780141439587" }],
        InternalCost = 42.50m
    };

    public static void Serializers()
    {
        var notice = SampleNotice();
        var xmlSettings = new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true };

        var sb = new StringBuilder();
        using (var writer = XmlWriter.Create(sb, xmlSettings))
        {
            new XmlSerializer(typeof(ShipmentNotice)).Serialize(writer, notice);
        }
        Console.WriteLine("XmlSerializer:");
        Console.WriteLine(sb);

        sb.Clear();
        using (var writer = XmlWriter.Create(sb, xmlSettings))
        {
            new DataContractSerializer(typeof(ShipmentNotice)).WriteObject(writer, notice);
        }
        Console.WriteLine("DataContractSerializer:");
        Console.WriteLine(sb);
    }

    public static void SerializerCache()
    {
        var notice = SampleNotice();
        int before = AppDomain.CurrentDomain.GetAssemblies().Length;
        for (int i = 0; i < 200; i++)
        {
            new XmlSerializer(typeof(ShipmentNotice)).Serialize(TextWriter.Null, notice);
        }
        int afterCached = AppDomain.CurrentDomain.GetAssemblies().Length;

        var root = new XmlRootAttribute("Notice") { Namespace = "urn:bookstore:shipping" };
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 200; i++)
        {
            new XmlSerializer(typeof(ShipmentNotice), root).Serialize(TextWriter.Null, notice);
        }
        int afterRoot = AppDomain.CurrentDomain.GetAssemblies().Length;
        Console.WriteLine($"Assemblies at start:                          {before}");
        Console.WriteLine($"After 200 x XmlSerializer(Type):              {afterCached}");
        Console.WriteLine($"After 200 x XmlSerializer(Type, XmlRootAttribute): {afterRoot} ({sw.ElapsedMilliseconds:N0} ms)");
        Console.WriteLine($"Dynamic assemblies: {AppDomain.CurrentDomain.GetAssemblies().Count(a => a.IsDynamic)}");
    }

    public static void Secure()
    {
        var defaults = new XmlReaderSettings();
        Console.WriteLine($"XmlReaderSettings defaults: DtdProcessing={defaults.DtdProcessing}");

        // A harmless internal DTD: no external references at all.
        const string withDtd = """
            <?xml version="1.0"?>
            <!DOCTYPE ShipmentNotice [ <!ENTITY carrier "ACME Cargo"> ]>
            <ShipmentNotice><Carrier>&carrier;</Carrier></ShipmentNotice>
            """;

        Try("XmlReader.Create (default)", () =>
        {
            using var reader = XmlReader.Create(new StringReader(withDtd));
            while (reader.Read()) { }
            return "parsed";
        });
        Try("XDocument.Parse", () => XDocument.Parse(withDtd).Root!.Element("Carrier")!.Value);
        Try("XmlDocument.LoadXml", () =>
        {
            var doc = new XmlDocument();
            doc.LoadXml(withDtd);
            return doc.DocumentElement!.InnerText;
        });
        Console.WriteLine($"MaxCharactersFromEntities default: {defaults.MaxCharactersFromEntities:N0}");
    }

    private static void Try(string label, Func<string> action)
    {
        try
        {
            Console.WriteLine($"{label}: {action()}");
        }
        catch (XmlException ex)
        {
            Console.WriteLine($"{label}: XmlException: {ex.Message}");
        }
    }
}
