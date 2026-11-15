using Bookstore.Analytics.Web.Analytics;

namespace Bookstore.Analytics.Tests.Web;

public class EventParameterGuardTests
{
    [Theory]
    [InlineData("email", "x")]
    [InlineData("search_term", "ada@example.com")]
    public void Rejects_personal_data(string key, string value) =>
        Assert.Throws<ArgumentException>(() =>
            EventParameterGuard.EnsureNoPersonalData(new Dictionary<string, object> { [key] = value }));

    [Fact]
    public void Accepts_plain_parameters() =>
        EventParameterGuard.EnsureNoPersonalData(new Dictionary<string, object> { ["file_name"] = "analytics-ve-olcum.pdf" });
}
