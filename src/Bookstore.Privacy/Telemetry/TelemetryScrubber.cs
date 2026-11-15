using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Bookstore.Privacy.Telemetry;

// For data that arrives from outside (browser error reports): the text is not ours, so we clean it before it is stored.
public static partial class TelemetryScrubber
{
    [GeneratedRegex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}")]
    private static partial Regex Email();

    // National ID (11 digits), card and phone numbers: any run of 10 or more digits.
    [GeneratedRegex(@"\d{10,}")]
    private static partial Regex LongNumber();

    public static string ScrubText(string text) =>
        LongNumber().Replace(Email().Replace(text, "[email]"), "[number]");

    // Keeps the path and the allowed query keys; drops the fragment and every other parameter.
    public static string ScrubUrl(string url, IReadOnlySet<string> allowedQueryKeys)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            return "[invalid-url]";
        }
        IEnumerable<string> kept = uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => allowedQueryKeys.Contains(Uri.UnescapeDataString(p.Split('=')[0])));
        string query = string.Join('&', kept);
        string path = ScrubText(Uri.UnescapeDataString(uri.AbsolutePath));
        return path + (query.Length > 0 ? "?" + query : "");
    }

    // IPv4: last octet zeroed (/24). IPv6: only the first 48 bits kept.
    public static string TruncateIp(IPAddress? address)
    {
        if (address is null)
        {
            return "unknown";
        }
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }
        byte[] bytes = address.GetAddressBytes();
        int keep = address.AddressFamily == AddressFamily.InterNetwork ? 3 : 6;
        Array.Clear(bytes, keep, bytes.Length - keep);
        return new IPAddress(bytes).ToString();
    }
}
