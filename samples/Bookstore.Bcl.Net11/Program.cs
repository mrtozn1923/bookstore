using System.Text;

// Usage: dotnet run --project samples/Bookstore.Bcl.Net11 -- <demo>
var demos = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase)
{
    ["runes"] = RuneDemos.Run,
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
