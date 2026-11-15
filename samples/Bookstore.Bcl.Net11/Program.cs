using System.Text;

// Usage: dotnet run --project samples/Bookstore.Bcl.Net11 -- <demo>
var demos = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase)
{
    ["runes"] = RuneDemos.Run,
    ["hexfloat"] = HexFloatDemos.Run,
    ["numbers"] = NumberDemos.Run,
    ["base64"] = Base64Demos.Run,
};

if (args.Length == 0 || !demos.TryGetValue(args[0], out Action? run))
{
    Console.Error.WriteLine("Demos: " + string.Join(", ", demos.Keys.Order(StringComparer.Ordinal)));
    return 1;
}

run();
return 0;

static class RuneDemos
{
    public static void Run()
    {
        string shelfLabel = "📚 Dune 📚";
        Rune books = new(0x1F4DA);

        Console.WriteLine($"Length: {shelfLabel.Length} chars, {shelfLabel.EnumerateRunes().Count()} runes");
        Console.WriteLine($"Contains(Rune)  : {shelfLabel.Contains(books)}");
        Console.WriteLine($"IndexOf(Rune)   : {shelfLabel.IndexOf(books)}");
        Console.WriteLine($"LastIndexOf     : {shelfLabel.LastIndexOf(books)}");
        Console.WriteLine($"Trim(Rune)      : [{shelfLabel.Trim(books).Trim()}]");
        Console.WriteLine($"Split(Rune)     : {string.Join(" | ", "Dune📚Emma📚Middlemarch".Split(books, StringSplitOptions.None))}");
        Console.WriteLine($"Replace(Rune, Rune): {shelfLabel.Replace(books, new Rune('*'))}");
    }
}

static class HexFloatDemos
{
    public static void Run()
    {
        double price = 0.1 + 0.2;
        var invariant = System.Globalization.CultureInfo.InvariantCulture;
        var tr = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");

        string hex = price.ToString("X", invariant);
        double back = double.Parse(hex, System.Globalization.NumberStyles.HexFloat, invariant);

        Console.WriteLine($"G17 (invariant): {price.ToString("G17", invariant)}");
        Console.WriteLine($"X (invariant)  : {hex}");
        Console.WriteLine($"X (tr-TR)      : {price.ToString("X", tr)}");
        Console.WriteLine($"Round-trip equal: {back == price}");
    }
}

static class NumberDemos
{
    public static void Run()
    {
        var invariant = System.Globalization.CultureInfo.InvariantCulture;
        var seeded = new Random(2026);

        long orderSuffix = seeded.NextInteger<long>(1_000_000L, 10_000_000L);
        byte shelf = seeded.NextInteger<byte>((byte)10);
        UInt128 big = seeded.NextInteger<UInt128>();
        float weight = seeded.NextBinaryFloat<float>();
        Console.WriteLine($"NextInteger<long>   : {orderSuffix}");
        Console.WriteLine($"NextInteger<byte>   : {shelf}");
        Console.WriteLine($"NextInteger<UInt128>: {big}");
        Console.WriteLine($"NextBinaryFloat<float>: {weight.ToString(invariant)}");

        Console.WriteLine($"(decimal)0.1    = {((decimal)0.1).ToString(invariant)}");
        Console.WriteLine($"(decimal)349.90 = {((decimal)349.90).ToString(invariant)}");
        Console.WriteLine($"349.90m         = {349.90m.ToString(invariant)}");
    }
}

static class Base64Demos
{
    public static void Run()
    {
        byte[] token = [0xFB, 0xEF, 0xFF, 0x3E, 0x3F, 0x00, 0x10];

        string encoded = System.Buffers.Text.Base64.EncodeToString(token);
        byte[] decoded = System.Buffers.Text.Base64.DecodeFromChars(encoded);
        Console.WriteLine($"Base64.EncodeToString : {encoded}");
        Console.WriteLine($"Base64.DecodeFromChars: {Convert.ToHexString(decoded)}");
        Console.WriteLine($"Same as Convert       : {encoded == Convert.ToBase64String(token)}");
        Console.WriteLine($"Uri.UriSchemeData     : {Uri.UriSchemeData}");
    }
}
