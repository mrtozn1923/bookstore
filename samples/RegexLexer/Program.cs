using System.Text.RegularExpressions;

string[] inputs =
[
    "price <= 300 and author = \"Jane Austen\"",
    "title = \"The \\\"Hobbit\\\"\"",
    "title = \"Dune",
];

foreach (string input in inputs)
{
    IEnumerable<string> tokens = NaiveLexer.Token().Matches(input)
        .Where(m => !m.Groups["ws"].Success)
        .Select(m => $"{KindOf(m)}:{m.Value}");
    Console.WriteLine($"{input,-42} => {string.Join("  ", tokens)}");
}

static string KindOf(Match m) =>
    new[] { "number", "string", "op", "word" }.First(g => m.Groups[g].Success);

static partial class NaiveLexer
{
    [GeneratedRegex("""(?<ws>\s+)|(?<number>\d+(\.\d+)?)|(?<string>"[^"]*")|(?<op><=|>=|!=|[<>=()])|(?<word>[A-Za-z_]\w*)""")]
    public static partial Regex Token();
}
