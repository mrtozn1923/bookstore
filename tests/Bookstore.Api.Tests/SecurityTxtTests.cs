using System.Globalization;
using System.Net;

namespace Bookstore.Api.Tests;

public sealed class SecurityTxtTests(BookstoreApiFactory factory)
{
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Security_txt_is_served_as_utf8_plain_text_with_the_required_fields()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/.well-known/security.txt", ct);
        string body = await response.Content.ReadAsStringAsync(ct);
        string[] lines = body.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        Assert.Contains(lines, l => l.StartsWith("Contact: ", StringComparison.Ordinal));
        Assert.Single(lines, l => l.StartsWith("Expires: ", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Security_txt_expires_in_less_than_a_year_and_not_within_30_days()
    {
        string body = await factory.CreateClient().GetStringAsync("/.well-known/security.txt", ct);
        string value = body.Split('\n').Single(l => l.StartsWith("Expires: ", StringComparison.Ordinal))["Expires: ".Length..];
        DateTimeOffset expires = DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
        TimeSpan left = expires - DateTimeOffset.UtcNow;

        // RFC 9116 recommends less than a year; 30 days before expiry this test fails and reminds us to renew.
        Assert.True(left < TimeSpan.FromDays(365), $"Expires is too far away: {value}");
        Assert.True(left > TimeSpan.FromDays(30), $"security.txt expires on {value}: renew it and ask the security champion to review the contacts.");
    }
}
