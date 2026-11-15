using System.Globalization;
using System.Text.RegularExpressions;

namespace Bookstore.Analytics.Logs;

// Parses the "combined" access log format used by nginx and Apache:
// ip - user [time] "METHOD path HTTP/x" status bytes "referer" "user-agent"
public static partial class CombinedLogParser
{
    [GeneratedRegex("""^\S+ \S+ \S+ \[(?<time>[^\]]+)\] "(?<method>[A-Z]+) (?<path>\S+) [^"]*" (?<status>\d{3}) \S+ "(?<referer>[^"]*)" "(?<ua>[^"]*)"$""")]
    private static partial Regex Line();

    public static AccessLogEntry? Parse(string line)
    {
        Match m = Line().Match(line);
        if (!m.Success)
        {
            return null;
        }

        DateTimeOffset time = DateTimeOffset.ParseExact(
            m.Groups["time"].Value, "dd/MMM/yyyy:HH:mm:ss zzz", CultureInfo.InvariantCulture);
        string referer = m.Groups["referer"].Value;
        string ua = m.Groups["ua"].Value;

        return new AccessLogEntry(
            time,
            m.Groups["method"].Value,
            m.Groups["path"].Value,
            int.Parse(m.Groups["status"].Value, CultureInfo.InvariantCulture),
            referer is "" or "-" ? null : referer,
            ua is "" or "-" ? null : ua);
    }
}
