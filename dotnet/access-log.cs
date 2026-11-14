using System.Globalization;
using System.Text.RegularExpressions;

// method, path, status and duration from one access log line
Regex line = new(@"^\S+ \S+ \S+ \[[^\]]*\] ""([A-Z]+) ([^ ""]+) [^""]*"" ([0-9]{3}) [0-9-]+ ""[^""]*"" ""[^""]*"" ([0-9]+\.[0-9]+)$");

foreach (string text in File.ReadLines("data/access.log"))
{
    Match m = line.Match(text);
    if (!m.Success)
    {
        Console.WriteLine($"no match: {text}");
        continue;
    }

    double seconds = double.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture);
    string flag = seconds >= 1 ? "  <-- slow" : "";
    Console.WriteLine($"{m.Groups[3].Value} {m.Groups[1].Value,-4} {m.Groups[2].Value,-36} {m.Groups[4].Value}s{flag}");
}
