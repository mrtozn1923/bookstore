using System.Text.RegularExpressions;

string log = File.ReadAllText("data/app.log");

// Count, Match, Matches
Console.WriteLine($"Count: {Patterns.OrderNumber.Count(log)}");
Console.WriteLine($"Match: {Patterns.OrderNumber.Match(log).Value}");
Console.WriteLine($"Matches: {string.Join(", ", Patterns.OrderNumber.Matches(log).Select(m => m.Value[^3..]))}");

// Replace with a MatchEvaluator: the replacement is computed per match
string firstLine = log[..log.IndexOf('\n')];
Console.WriteLine(Patterns.OrderNumber.Replace(firstLine, m => m.Value[..9] + "****" + m.Value[^2..]));

// Split keeps empty entries; EnumerateSplits (.NET 9) returns ranges instead of strings
string titles = "Dune; Emma ,Middlemarch;;The Hobbit";
Console.WriteLine(string.Join(" | ", Patterns.Separator.Split(titles)));
foreach (Range range in Patterns.Separator.EnumerateSplits(titles))
    Console.Write($"[{titles[range]}]");
Console.WriteLine();

// IsMatch on a ReadOnlySpan<char>: a slice of the line, no Substring
ReadOnlySpan<char> lastFifteen = firstLine.AsSpan()[^15..];
Console.WriteLine($"IsMatch(span): {Patterns.OrderNumber.IsMatch(lastFifteen)}");

// allocations: Matches creates Match objects and strings, EnumerateMatches does not
Measure("Matches", () => { int n = 0; foreach (Match m in Patterns.OrderNumber.Matches(log)) n += m.Value.Length; return n; });
Measure("EnumerateMatches", () => { int n = 0; foreach (ValueMatch m in Patterns.OrderNumber.EnumerateMatches(log)) n += m.Length; return n; });

static void Measure(string name, Func<int> work)
{
    work(); // warm-up, so the JIT does not count
    long before = GC.GetAllocatedBytesForCurrentThread();
    int chars = work();
    long after = GC.GetAllocatedBytesForCurrentThread();
    Console.WriteLine($"{name,-17} {chars} chars, {after - before} bytes allocated");
}

static partial class Patterns
{
    [GeneratedRegex("BKS-[0-9]{4}-[0-9]{6}")]
    public static partial Regex OrderNumber { get; }

    [GeneratedRegex(@"\s*[;,]\s*")]
    public static partial Regex Separator { get; }
}
