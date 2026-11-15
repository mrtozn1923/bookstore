using Bookstore.Analytics.Logs;

namespace Bookstore.Analytics.Tests.Logs;

public class TrafficClassifierTests
{
    private readonly TrafficClassifier classifier = new("www.bookstore.example");

    private static AccessLogEntry Get(string path, string? referer, string ua = "Mozilla/5.0", int status = 200) =>
        new(DateTimeOffset.UnixEpoch, "GET", path, status, referer, ua);

    [Theory]
    [InlineData("https://www.google.com/")]
    [InlineData("https://www.google.com.tr/")]
    [InlineData("https://www.google.co.uk/")]
    [InlineData("android-app://com.google.android.googlequicksearchbox/https/www.google.com")]
    public void Google_referers_are_search(string referer) =>
        Assert.Equal(TrafficSource.GoogleSearch, classifier.Classify(Get("/blog/a", referer)));

    [Theory]
    [InlineData("https://www.bing.com/", TrafficSource.OtherReferral)]
    [InlineData("https://mail.google.com/", TrafficSource.OtherReferral)]
    [InlineData("https://www.bookstore.example/blog/b", TrafficSource.InternalNavigation)]
    [InlineData(null, TrafficSource.NoReferer)]
    public void Other_referers(string? referer, TrafficSource expected) =>
        Assert.Equal(expected, classifier.Classify(Get("/blog/a", referer)));

    [Fact]
    public void Assets_redirects_and_errors_are_not_landings()
    {
        Assert.Equal(TrafficSource.NotALanding, classifier.Classify(Get("/css/site.css", "https://www.google.com/")));
        Assert.Equal(TrafficSource.NotALanding, classifier.Classify(Get("/old", "https://www.google.com/", status: 301)));
        Assert.Equal(TrafficSource.NotALanding, classifier.Classify(Get("/missing", "https://www.google.com/", status: 404)));
    }

    [Fact]
    public void Googlebot_user_agent_is_only_a_claim() =>
        Assert.Equal(TrafficSource.ClaimsToBeBot,
            classifier.Classify(Get("/blog/a", null, "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)")));

    [Fact]
    public void Parser_reads_combined_format()
    {
        AccessLogEntry? entry = CombinedLogParser.Parse(
            "127.0.0.1 - - [09/Nov/2026:08:00:00 +0000] \"GET /blog/a?x=1 HTTP/1.1\" 200 1234 \"https://www.google.com/\" \"Mozilla/5.0\"");

        Assert.NotNull(entry);
        Assert.Equal("/blog/a?x=1", entry.Path);
        Assert.Equal("https://www.google.com/", entry.Referer);
        Assert.Equal(new DateTimeOffset(2026, 11, 9, 8, 0, 0, TimeSpan.Zero), entry.Time);
    }
}
