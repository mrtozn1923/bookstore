using System.Text.RegularExpressions;

string line = File.ReadLines("data/access.log").First();

// 1. one line: correct, but hard to review
Regex dense = new(@"^(?<ip>\S+) \S+ \S+ \[(?<time>[^\]]+)\] ""(?<method>[A-Z]+) (?<path>\S+) [^""]*"" (?<status>[0-9]{3}) (?<bytes>[0-9]+) ""[^""]*"" ""[^""]*"" (?<seconds>[0-9.]+)$");

// 2. IgnorePatternWhitespace: spaces are ignored, # starts a comment, a real space is written as \x20
Regex commented = new("""
    ^(?<ip>\S+)                  # client address
    \x20\S+\x20\S+               # identity and user, always "-"
    \x20\[(?<time>[^\]]+)\]      # [15/Nov/2026:10:42:07 +0000]
    \x20"(?<method>[A-Z]+)       # "GET
    \x20(?<path>\S+)             #  /api/books/978-0-441-17271-9
    \x20[^"]*"                   #  HTTP/1.1"
    \x20(?<status>[0-9]{3})      # 200
    \x20(?<bytes>[0-9]+)         # response size
    \x20"[^"]*"\x20"[^"]*"       # referer and user agent
    \x20(?<seconds>[0-9.]+)$     # duration in seconds
    """, RegexOptions.IgnorePatternWhitespace);

// 3. built from named parts at compile time: AccessLog.Line below
foreach (Regex regex in new[] { dense, commented, AccessLog.Line })
{
    Match m = regex.Match(line);
    Console.WriteLine($"{m.Groups["method"].Value} {m.Groups["path"].Value} {m.Groups["status"].Value} {m.Groups["seconds"].Value}");
}

// in (?x) a plain space is ignored: this pattern looks for "GET/api"
Console.WriteLine($"(?x) with a plain space: {Regex.IsMatch(line, "(?x)GET /api")}");

static partial class AccessLog
{
    private const string Ip = @"(?<ip>\S+)";
    private const string Identity = @"\S+ \S+";
    private const string Time = @"\[(?<time>[^\]]+)\]";
    private const string Request = @"""(?<method>[A-Z]+) (?<path>\S+) [^""]*""";
    private const string Status = "(?<status>[0-9]{3})";
    private const string Bytes = "(?<bytes>[0-9]+)";
    private const string Quoted = @"""[^""]*""";
    private const string Seconds = @"(?<seconds>[0-9.]+)";

    [GeneratedRegex($"^{Ip} {Identity} {Time} {Request} {Status} {Bytes} {Quoted} {Quoted} {Seconds}$")]
    public static partial Regex Line { get; }
}
