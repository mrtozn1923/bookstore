using System.Globalization;
using Bookstore.Bcl.Text;

namespace Bookstore.Bcl.Samples.Formatting;

public static class FormatDemos
{
    public static IEnumerable<(string Name, Action Run)> All =>
    [
        ("format-guess", Guess),
        ("format-numbers", Numbers),
        ("format-custom", CustomNumbers),
        ("format-dates", Dates),
        ("format-date-pitfalls", DatePitfalls),
        ("format-align", Alignment),
        ("format-money", Money),
        ("format-invariant", Invariant),
        ("format-isbn", IsbnFormatting),
        ("format-table", Table),
    ];

    private static readonly DateTimeOffset OrderPlaced = new(2026, 11, 14, 9, 5, 7, TimeSpan.FromHours(3));

    private static void Guess()
    {
        decimal price = 1234.5m;
        DateTime placed = new(2026, 11, 14, 9, 5, 0);

        Console.WriteLine(price.ToString("C"));
        Console.WriteLine(placed.ToString("yyyy-mm-dd"));
        Console.WriteLine(placed.ToString("dd/MM/yyyy"));
    }

    private static void Numbers()
    {
        decimal price = 1234.5m;
        Row("C", price.ToString("C"));
        Row("C0", price.ToString("C0"));
        Row("N", price.ToString("N"));
        Row("N0", price.ToString("N0"));
        Row("F3", price.ToString("F3"));
        Row("E2", price.ToString("E2"));
        Row("G", price.ToString("G"));
        Row("P1 (0.125)", 0.125m.ToString("P1"));
        Row("D6 (42)", 42.ToString("D6"));
        Row("X4 (255)", 255.ToString("X4"));
        Row("x (255)", 255.ToString("x"));
        Row("B8 (5)", 5.ToString("B8"));
        Row("N2 (-1234.5)", (-1234.5m).ToString("N2"));

        double sum = 0.1 + 0.2;
        Row("double default", sum.ToString());
        Row("double R", sum.ToString("R"));
        Row("double G17", sum.ToString("G17"));
        Row("double F2", sum.ToString("F2"));

        static void Row(string label, string value) => Console.WriteLine($"{label,-15}{value}");
    }

    private static void CustomNumbers()
    {
        CultureInfo culture = CultureInfo.CurrentCulture;
        decimal[] values = [1234.5m, -1234.5m, 0m, 0.075m];
        string[] formats = ["0000.00", "#,##0.00", "#.##", "0.##", "#,##0,K", "0.0%", "#,##0.00;(#,##0.00);-", "\\#0"];

        foreach (string format in formats)
        {
            string cells = string.Join(" | ", values.Select(v => v.ToString(format, culture).PadLeft(12)));
            Console.WriteLine($"{format,-24}{cells}");
        }
    }

    private static void Dates()
    {
        string[] formats = ["d", "D", "f", "g", "M", "Y", "o", "s", "u", "R", "yyyy-MM-dd", "dd.MM.yyyy HH:mm", "d MMMM yyyy, dddd", "hh:mm tt", "HH:mm:ss zzz"];
        foreach (string format in formats)
            Console.WriteLine($"{format,-20}{OrderPlaced.ToString(format)}");
    }

    private static void DatePitfalls()
    {
        DateTime placed = new(2026, 11, 14, 9, 5, 0);
        CultureInfo tr = CultureInfo.GetCultureInfo("tr-TR");

        Console.WriteLine($"yyyy-mm-dd          {placed.ToString("yyyy-mm-dd", tr)}");
        Console.WriteLine($"yyyy-MM-dd          {placed.ToString("yyyy-MM-dd", tr)}");
        Console.WriteLine($"dd/MM/yyyy          {placed.ToString("dd/MM/yyyy", tr)}");
        Console.WriteLine($"dd'/'MM'/'yyyy      {placed.ToString("dd'/'MM'/'yyyy", tr)}");
        Console.WriteLine($"hh:mm (21:35)       {placed.AddHours(12.5).ToString("hh:mm", tr)}");
        Console.WriteLine($"HH:mm (21:35)       {placed.AddHours(12.5).ToString("HH:mm", tr)}");
        Console.WriteLine($"o (Kind={placed.Kind})  {placed.ToString("o")}");
        Console.WriteLine($"o (Kind=Utc)          {DateTime.SpecifyKind(placed, DateTimeKind.Utc).ToString("o")}");
    }

    private static void Alignment()
    {
        (string Title, int Quantity, decimal Price)[] lines = [("Dune", 2, 349.90m), ("Middlemarch", 1, 1249.50m), ("The Hobbit", 12, 89m)];

        Console.WriteLine($"{"Title",-14}{"Qty",4}{"Price",12}{"Total",12}");
        foreach (var (title, quantity, price) in lines)
            Console.WriteLine($"{title,-14}{quantity,4}{price,12:N2}{quantity * price,12:N2}");
        Console.WriteLine($"{"",-14}{"",4}{"",12}{lines.Sum(l => l.Quantity * l.Price),12:N2}");

        Console.WriteLine(string.Format("[{0,8}] [{0,-8}] [{1,8:P0}]", "Dune", 0.1));
        Console.WriteLine($"[{"Middlemarch",6}]");
    }

    private static void Money()
    {
        decimal total = 1234.50m;
        CultureInfo tr = CultureInfo.GetCultureInfo("tr-TR");
        CultureInfo en = CultureInfo.GetCultureInfo("en-US");
        CultureInfo de = CultureInfo.GetCultureInfo("de-DE");

        Console.WriteLine($"tr-TR C       : {total.ToString("C", tr)}");
        Console.WriteLine($"en-US C       : {total.ToString("C", en)}");
        Console.WriteLine($"de-DE C       : {total.ToString("C", de)}");
        Console.WriteLine($"tr-TR invoice : {FormatMoney(total, "TRY", tr)}");
        Console.WriteLine($"en-US invoice : {FormatMoney(total, "TRY", en)}");
        Console.WriteLine($"en-US code    : {FormatWithCode(total, "TRY", en)}");
        Console.WriteLine($"de-DE EUR     : {FormatMoney(total, "EUR", de)}");
        Console.WriteLine($"tr-TR EUR     : {FormatMoney(total, "EUR", tr)}");
        Console.WriteLine($"tr-TR n $     : {FormatMoney(total, "TRY", tr, symbolAfter: true)}");
        Console.WriteLine($"tr-TR -n $    : {FormatMoney(-total, "TRY", tr, symbolAfter: true)}");

        RegionInfo turkey = new("TR");
        Console.WriteLine($"RegionInfo TR : {turkey.ISOCurrencySymbol} {turkey.CurrencySymbol} {turkey.CurrencyEnglishName}");
    }

    // The currency comes from the order; only the number layout comes from the culture.
    public static string FormatMoney(decimal amount, string currencyCode, CultureInfo culture, bool symbolAfter = false)
    {
        var numberFormat = (NumberFormatInfo)culture.NumberFormat.Clone();
        if (symbolAfter)
        {
            numberFormat.CurrencyPositivePattern = 3; // "n $"
            numberFormat.CurrencyNegativePattern = 8; // "-n $"
        }
        numberFormat.CurrencySymbol = currencyCode switch
        {
            "TRY" => "₺",
            "EUR" => "€",
            _ => currencyCode,
        };
        return amount.ToString("C2", numberFormat);
    }

    public static string FormatWithCode(decimal amount, string currencyCode, CultureInfo culture) =>
        string.Create(culture, $"{currencyCode} {amount:N2}");

    private static void Invariant()
    {
        decimal price = 1234.5m;
        DateTime placed = new(2026, 11, 14, 9, 5, 0);
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");

        string line = $"{placed:d};{price}";
        Console.WriteLine($"Current (tr-TR)            : {line}");
        Console.WriteLine($"FormattableString.Invariant: {FormattableString.Invariant($"{placed:yyyy-MM-dd};{price}")}");
        Console.WriteLine($"string.Create(Invariant)   : {string.Create(CultureInfo.InvariantCulture, $"{placed:yyyy-MM-dd};{price}")}");

        string written = price.ToString();
        decimal readBack = decimal.Parse(written, CultureInfo.GetCultureInfo("en-US"));
        Console.WriteLine($"Written by tr-TR \"{written}\", read by en-US: {readBack}");
    }

    private static void IsbnFormatting()
    {
        Isbn dune = Isbn.Parse("ISBN 978-0-441-17271-9");

        Console.WriteLine($"{dune}");
        Console.WriteLine($"{dune:P}");
        Console.WriteLine($"[{dune,22:P}]");
        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0:G} / {0:P}", dune));

        Span<char> buffer = stackalloc char[32];
        if (dune.TryFormat(buffer, out int written, "P", null))
            Console.WriteLine($"TryFormat wrote {written} chars: {buffer[..written]}");

        Span<char> tooSmall = stackalloc char[8];
        Console.WriteLine($"TryFormat into 8 chars: {dune.TryFormat(tooSmall, out _, "P", null)}");

        try
        {
            Console.WriteLine($"{dune:X}");
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void Table()
    {
        decimal amount = 1234.5m;
        string[] numberFormats = ["C", "N", "N2", "F2", "P1", "#,##0.00", "0.##"];
        string[] dateFormats = ["d", "D", "g", "s", "dd/MM/yyyy", "d MMMM yyyy"];
        CultureInfo[] cultures = [CultureInfo.GetCultureInfo("tr-TR"), CultureInfo.GetCultureInfo("en-US"), CultureInfo.InvariantCulture];

        foreach (string format in numberFormats)
        {
            decimal value = format == "P1" ? 0.125m : amount;
            Console.WriteLine($"{format,-12}| " + string.Join(" | ", cultures.Select(c => value.ToString(format, c))));
        }
        foreach (string format in dateFormats)
            Console.WriteLine($"{format,-12}| " + string.Join(" | ", cultures.Select(c => OrderPlaced.DateTime.ToString(format, c))));
    }
}
