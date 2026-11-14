using System.Text.RegularExpressions;

string level = "2026-11-15T10:42:12Z err Bookstore.Payments Payment 4713 declined";

// (?i) for the whole pattern, (?i:...) for one part, (?-i) to turn it off again
Console.WriteLine(Regex.IsMatch(level, @"(?i)ERR bookstore\.payments"));
Console.WriteLine(Regex.IsMatch(level, @"(?i:ERR) bookstore\.payments"));
Console.WriteLine(Regex.IsMatch(level, @"(?i)ERR (?-i)Bookstore\.Payments"));

// (?x) ignores pattern whitespace and allows # comments; (?n) captures only named groups
Regex order = new(@"(?xn)
    ^BKS -
    (?<year> [0-9]{4} ) -    # year
    ([0-9]{6}) $             # sequence, not captured because of n
");
Match m = order.Match("BKS-2026-000123");
Console.WriteLine($"{m.Success} groups={m.Groups.Count} year={m.Groups["year"].Value}");

// \w and \p{L} include Turkish letters in .NET
Console.WriteLine($"{Regex.IsMatch("Çalıkuşu", @"^\w+$")} {Regex.IsMatch("Çalıkuşu", @"^\p{L}+$")} {Regex.IsMatch("Çalıkuşu", @"^\p{Lu}\p{Ll}+$")}");
