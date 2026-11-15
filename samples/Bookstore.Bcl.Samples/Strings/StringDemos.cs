using System.Buffers;
using System.Globalization;
using System.Text;
using Bookstore.Bcl.Text;

namespace Bookstore.Bcl.Samples.Strings;

public static class StringDemos
{
    public static IEnumerable<(string Name, Action Run)> All =>
    [
        ("strings-guess", Guess),
        ("strings-search", Search),
        ("strings-ignorecase", IgnoreCase),
        ("strings-split", Split),
        ("strings-join", Join),
        ("strings-trim", TrimPadReplace),
        ("strings-ranges", Ranges),
        ("strings-slug", SlugDemo),
        ("strings-isbn", IsbnDemo),
        ("strings-searchvalues", SearchValuesDemo),
        ("strings-compositeformat", CompositeFormatDemo),
    ];

    private static void Guess()
    {
        // A fixed-width title field from the legacy accounting system, padded with NUL characters.
        string field = "Dune\0\0\0\0";
        Console.WriteLine(field.IndexOf('\0'));
        Console.WriteLine(field.IndexOf("\0"));
        Console.WriteLine("Dune,,Emma,".Split(',').Length);
    }

    private static void Search()
    {
        string field = "Dune\0\0\0\0";

        Row("IndexOf('\\0')", field.IndexOf('\0'));
        Row("IndexOf(\"\\0\")", field.IndexOf("\0"));
        Row("IndexOf(\"\\0\", Ordinal)", field.IndexOf("\0", StringComparison.Ordinal));
        Row("Contains(\"\\0\")", field.Contains("\0"));
        Row("Replace(\"\\0\", \"\")", $"[{field.Replace("\0", "")}]");

        // U+00AD soft hyphen: invisible, often comes with text copied from a web page.
        string pasted = "Middle\u00ADmarch";
        Row("StartsWith(\"Middlemarch\")", pasted.StartsWith("Middlemarch"));
        Row("StartsWith(..., Ordinal)", pasted.StartsWith("Middlemarch", StringComparison.Ordinal));
        Row("Equals(\"Middlemarch\")", pasted.Equals("Middlemarch"));
        Row("Contains(\"Middlemarch\")", pasted.Contains("Middlemarch"));

        static void Row(string label, object value) => Console.WriteLine($"{label,-26}: {value}");
    }

    private static void IgnoreCase()
    {
        string title = "THE HOBBIT";
        Console.WriteLine($"{CultureInfo.CurrentCulture.Name,-6} CurrentCultureIgnoreCase: {title.IndexOf("hobbit", StringComparison.CurrentCultureIgnoreCase)}");
        Console.WriteLine($"{CultureInfo.CurrentCulture.Name,-6} OrdinalIgnoreCase       : {title.IndexOf("hobbit", StringComparison.OrdinalIgnoreCase)}");
        Console.WriteLine($"{CultureInfo.CurrentCulture.Name,-6} ToLower()               : {title.ToLower()}");
    }

    private static void Split()
    {
        string input = "Dune, Emma,, The Hobbit , ";

        Show("Split(',')", input.Split(','));
        Show("RemoveEmptyEntries", input.Split(',', StringSplitOptions.RemoveEmptyEntries));
        Show("TrimEntries", input.Split(',', StringSplitOptions.TrimEntries));
        Show("Remove | Trim", input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        Show("count: 2", "BS-2026-0001234".Split('-', 2));
        Show("string separator", "Dune -- Emma -- Middlemarch".Split(" -- "));
        Show("any of ; and |", "Dune;Emma|Middlemarch".Split([';', '|']));

        static void Show(string label, string[] parts) =>
            Console.WriteLine($"{label,-18} {parts.Length} -> [{string.Join(" | ", parts.Select(p => $"\"{p}\""))}]");
    }

    private static void Join()
    {
        string[] titles = Enumerable.Range(1, 1_000).Select(i => $"Book {i:D4}").ToArray();

        long before = GC.GetAllocatedBytesForCurrentThread();
        string csv = "";
        foreach (string title in titles)
            csv += title + ",";
        long concatLoop = GC.GetAllocatedBytesForCurrentThread() - before;

        before = GC.GetAllocatedBytesForCurrentThread();
        var builder = new StringBuilder();
        foreach (string title in titles)
            builder.Append(title).Append(',');
        string built = builder.ToString();
        long stringBuilder = GC.GetAllocatedBytesForCurrentThread() - before;

        before = GC.GetAllocatedBytesForCurrentThread();
        string joined = string.Join(',', titles);
        long join = GC.GetAllocatedBytesForCurrentThread() - before;

        Console.WriteLine($"+= in a loop  : {concatLoop,12:N0} bytes, length {csv.Length}");
        Console.WriteLine($"StringBuilder : {stringBuilder,12:N0} bytes, length {built.Length}");
        Console.WriteLine($"string.Join   : {join,12:N0} bytes, length {joined.Length}");

        Console.WriteLine(string.Join(", ", "Dune", "Emma", "Middlemarch"));
        Console.WriteLine(string.Concat("BS-", "2026", "-", "0001234"));
        Console.WriteLine(string.Join(" / ", new List<int> { 3, 1, 2 }));

        int orderNo = 1234;
        string viaCreate = string.Create(10, orderNo, static (span, number) =>
        {
            "BS-".CopyTo(span);
            number.TryFormat(span[3..], out _, "D7", CultureInfo.InvariantCulture);
        });
        Console.WriteLine(viaCreate);
    }

    private static void TrimPadReplace()
    {
        Console.WriteLine($"[{"  Dune \t\r\n".Trim()}]");
        Console.WriteLine($"[{"--Dune..".Trim('-', '.')}]");
        Console.WriteLine($"[{"/books/dune/".TrimEnd('/')}]");
        Console.WriteLine($"[{"000123".TrimStart('0')}]");

        Console.WriteLine($"[{"123".PadLeft(7, '0')}]");
        Console.WriteLine($"[{"Dune".PadRight(12)}|{"Emma".PadLeft(8)}]");

        string note = "Gift wrap, GIFT card, gift note";
        Console.WriteLine(note.Replace("gift", "present"));
        Console.WriteLine(note.Replace("gift", "present", StringComparison.OrdinalIgnoreCase));
        Console.WriteLine("978-0-441-17271-9".Replace("-", ""));
        Console.WriteLine("Line1\r\nLine2\nLine3".ReplaceLineEndings(" | "));

        Console.WriteLine("BS0001234".Insert(2, "-"));
        Console.WriteLine("BS-0001234-DRAFT".Remove(10));
        Console.WriteLine("Dune (Ace, 1990)".Remove(4, 12));
    }

    private static void Ranges()
    {
        string orderNumber = "BS-2026-0001234";

        string year1 = orderNumber.Substring(3, 4);
        string year2 = orderNumber[3..7];
        ReadOnlySpan<char> yearSpan = orderNumber.AsSpan(3, 4);
        string last = orderNumber[^7..];

        Console.WriteLine($"{year1} {year2} {yearSpan.ToString()} {last}");

        long before = GC.GetAllocatedBytesForCurrentThread();
        int sum = 0;
        for (int i = 0; i < 10_000; i++)
            sum += int.Parse(orderNumber.Substring(8));
        long withSubstring = GC.GetAllocatedBytesForCurrentThread() - before;

        before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10_000; i++)
            sum += int.Parse(orderNumber.AsSpan(8));
        long withSpan = GC.GetAllocatedBytesForCurrentThread() - before;

        Console.WriteLine($"Substring + int.Parse : {withSubstring:N0} bytes");
        Console.WriteLine($"AsSpan + int.Parse    : {withSpan:N0} bytes");
        Console.WriteLine(sum);
    }

    private static void SlugDemo()
    {
        string[] titles = ["Kürk Mantolu Madonna", "İnce Memed", "ISTANBUL HATIRALAR VE ŞEHİR", "The Hobbit: or There and Back Again", "  Dune -- Part 1 "];
        CultureInfo tr = CultureInfo.GetCultureInfo("tr-TR");
        CultureInfo en = CultureInfo.GetCultureInfo("en-US");

        foreach (string title in titles)
        {
            Console.WriteLine($"tr-TR naive : {title.ToLower(tr).Replace(" ", "-")}");
            Console.WriteLine($"en-US naive : {title.ToLower(en).Replace(" ", "-")}");
            Console.WriteLine($"Slug        : {Slug.FromTitle(title)}");
        }

        Console.WriteLine(Slug.FromTitle("Saatleri Ayarlama Enstitüsü", maxLength: 20));
    }

    private static void IsbnDemo()
    {
        string?[] inputs =
        [
            "ISBN 978-0-441-17271-9",
            "  isbn: 978 0441 172719 ",
            "9780441172719",
            "ISBN 978-0-441-17271-8",
            "978-0-441-17271",
            "ISBN 978-0-441-17271-9X",
            "",
            null,
        ];

        foreach (string? input in inputs)
        {
            string shown = input is null ? "null" : $"\"{input}\"";
            Console.WriteLine(IsbnNormalizer.TryNormalize(input, out string? isbn)
                ? $"{shown,-28} -> {isbn}"
                : $"{shown,-28} -> invalid");
        }
    }

    private static readonly SearchValues<char> UnsafeSearchChars = SearchValues.Create("<>\"'%;()&+");

    private static void SearchValuesDemo()
    {
        string[] queries = ["dune", "dune<script>", "emma's", "middlemarch 1871"];
        foreach (string query in queries)
        {
            int index = query.AsSpan().IndexOfAny(UnsafeSearchChars);
            Console.WriteLine($"{query,-18} IndexOfAny = {index,2}, ContainsAny = {query.AsSpan().ContainsAny(UnsafeSearchChars)}");
        }

        SearchValues<string> stopWords = SearchValues.Create(["the", "and", "of"], StringComparison.OrdinalIgnoreCase);
        string title = "The Lord of the Rings";
        Console.WriteLine($"First stop word at {title.AsSpan().IndexOfAny(stopWords)}");
    }

    private static readonly CompositeFormat InvoiceLine = CompositeFormat.Parse("{0,-12}{1,3} x {2,10:N2} = {3,10:N2}");

    private static void CompositeFormatDemo()
    {
        CultureInfo tr = CultureInfo.GetCultureInfo("tr-TR");
        (string Title, int Quantity, decimal Price)[] lines = [("Dune", 2, 349.90m), ("Middlemarch", 1, 1249.50m)];

        foreach (var line in lines)
            Console.WriteLine(string.Format(tr, InvoiceLine, line.Title, line.Quantity, line.Price, line.Quantity * line.Price));

        Console.WriteLine($"Arguments expected: {InvoiceLine.MinimumArgumentCount}");

        try
        {
            CompositeFormat.Parse("{0} x {1");
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"{ex.GetType().Name}: {ex.Message}");
        }
    }
}
