using System.Text.RegularExpressions;

namespace Bookstore.Analytics.Logs;

public enum TrafficSource
{
    NotALanding,
    ClaimsToBeBot,
    GoogleSearch,
    OtherReferral,
    InternalNavigation,
    NoReferer,
}

public sealed partial class TrafficClassifier(string siteHost)
{
    private static readonly string[] AssetExtensions =
        [".css", ".js", ".png", ".jpg", ".jpeg", ".webp", ".avif", ".svg", ".ico", ".woff2", ".map", ".json", ".xml", ".txt"];

    private static readonly string[] BotTokens = ["Googlebot", "bingbot", "GPTBot", "ClaudeBot", "PerplexityBot"];

    // www.google.com, www.google.com.tr, www.google.co.uk ...
    [GeneratedRegex(@"^(www\.)?google(\.[a-z]{2,3}){1,2}$")]
    private static partial Regex GoogleHost();

    private const string GoogleAndroidApp = "android-app://com.google.android.googlequicksearchbox/";

    public TrafficSource Classify(AccessLogEntry entry)
    {
        if (entry.Method != "GET" || entry.Status != 200 || IsAsset(entry.Path))
        {
            return TrafficSource.NotALanding;
        }

        if (entry.UserAgent is { } ua && BotTokens.Any(t => ua.Contains(t, StringComparison.OrdinalIgnoreCase)))
        {
            // only what the user agent claims; verify with reverse DNS
            return TrafficSource.ClaimsToBeBot;
        }

        if (entry.Referer is null)
        {
            return TrafficSource.NoReferer;
        }

        if (entry.Referer.StartsWith(GoogleAndroidApp, StringComparison.Ordinal))
        {
            return TrafficSource.GoogleSearch;
        }

        if (!Uri.TryCreate(entry.Referer, UriKind.Absolute, out Uri? referer))
        {
            return TrafficSource.OtherReferral;
        }

        if (referer.Host.Equals(siteHost, StringComparison.OrdinalIgnoreCase))
        {
            return TrafficSource.InternalNavigation;
        }

        return GoogleHost().IsMatch(referer.Host) ? TrafficSource.GoogleSearch : TrafficSource.OtherReferral;
    }

    private static bool IsAsset(string path)
    {
        string clean = path.Split('?', 2)[0];
        return AssetExtensions.Any(ext => clean.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }
}
