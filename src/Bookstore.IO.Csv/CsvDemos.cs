using System.Globalization;
using System.Text;
using Bookstore.IO.Csv.Bank;
using Bookstore.IO.Csv.Export;
using Bookstore.Ordering;

namespace Bookstore.IO.Csv;

public static class CsvDemos
{
    private const string NotesCsv =
        "OrderNumber,CustomerName,Note,Total\r\n" +
        "BS-0000001,Ada,Gift wrap,349.90\r\n" +
        "BS-0000002,\"Lovelace, Ada\",\"Leave at the door, ring twice\",189.50\r\n" +
        "BS-0000003,Grace,\"Deliver to \"\"Building B\"\"\r\n2nd floor\",259.00\r\n";

    public static void Split()
    {
        Console.WriteLine("string.Split:");
        foreach (string line in NotesCsv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Skip(1))
        {
            string[] fields = line.Split(',');
            Console.WriteLine($"  {fields.Length} fields, Total = {fields[^1]}");
        }

        Console.WriteLine("RFC 4180:");
        using var reader = new StringReader(NotesCsv);
        Rfc4180.ReadRecord(reader); // header
        while (Rfc4180.ReadRecord(reader) is { } fields)
        {
            Console.WriteLine($"  {fields.Count} fields, Total = {fields[3]}, Note = {fields[2].ReplaceLineEndings("\\n")}");
        }
    }

    public static void Culture()
    {
        var tr = CultureInfo.GetCultureInfo("tr-TR");
        var inv = CultureInfo.InvariantCulture;
        decimal amount = 1234.5m;
        Console.WriteLine($"ListSeparator tr-TR: '{tr.TextInfo.ListSeparator}', invariant: '{inv.TextInfo.ListSeparator}'");
        Console.WriteLine($"ToString(\"N2\") tr-TR: {amount.ToString("N2", tr)}, invariant: {amount.ToString("N2", inv)}");
        Console.WriteLine($"decimal.Parse(\"1234.50\", tr-TR) = {decimal.Parse("1234.50", tr)}");
        Console.WriteLine($"decimal.Parse(\"1.234,50\", tr-TR) = {decimal.Parse("1.234,50", tr)}");
        Try("decimal.Parse(\"1.234,50\", invariant)", () => decimal.Parse("1.234,50", inv).ToString(inv));
        Console.WriteLine($"DateTime.Parse(\"03.11.2026\", tr-TR) = {DateTime.Parse("03.11.2026", tr):yyyy-MM-dd}");
        Console.WriteLine($"DateTime.Parse(\"03.11.2026\", invariant) = {DateTime.Parse("03.11.2026", inv):yyyy-MM-dd}");

        static void Try(string label, Func<string> action)
        {
            try { Console.WriteLine($"{label} = {action()}"); }
            catch (Exception ex) { Console.WriteLine($"{label}: {ex.GetType().Name}: {ex.Message}"); }
        }
    }

    public static void BankEncoding()
    {
        try
        {
            Encoding.GetEncoding(1254);
        }
        catch (NotSupportedException ex)
        {
            Console.WriteLine($"GetEncoding(1254) without provider: {ex.Message}");
        }
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding windows1254 = Encoding.GetEncoding(1254);

        string path = PrepareBankFile();
        Console.WriteLine($"UTF-8 (default): {File.ReadLines(path).First()}");
        Console.WriteLine($"Windows-1254:    {File.ReadLines(path, windows1254).First()}");
        Console.WriteLine($"                 {File.ReadLines(path, windows1254).Skip(1).First()}");
    }

    public static void BankImport()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        string path = PrepareBankFile();
        ImportResult result = BankFile.Import(path);
        Console.WriteLine($"Imported {result.Imported:N0} payments, total {result.Total:N2} TRY, {result.Errors.Count} errors");
        foreach (string error in result.Errors) Console.WriteLine($"  {error}");
    }

    public static void Export(string mode)
    {
        Directory.CreateDirectory("out");
        var orders = SampleOrders.Generate(5).ToList();
        // Biri müşteri adına formül yazmış.
        orders[1] = orders[1] with { CustomerName = "=1+1" };
        orders[3] = orders[3] with { CustomerName = "@Grace" };

        var options = mode == "default"
            ? CsvHelper.CsvWriterOptions.Default
            : OrderCsvExport.ExcelTurkish;
        string path = Path.Join("out", $"orders-{mode}.csv");
        OrderCsvExport.Write(path, orders, options);

        byte[] bytes = File.ReadAllBytes(path);
        Console.WriteLine($"First bytes: {Convert.ToHexString(bytes, 0, 4)}");
        Console.Write(File.ReadAllText(path));
    }

    private static string PrepareBankFile()
    {
        Directory.CreateDirectory("out");
        string path = Path.Join("out", "bank-payments.csv");
        if (!File.Exists(path))
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            BankFile.WriteSample(path, 1000);
        }
        return path;
    }
}
