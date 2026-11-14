using System.Text;
using Bookstore.Api.Payments;

namespace Bookstore.Api.Tests;

public class WebhookSignatureTests
{
    private const string OldSecret = "old-secret-0123456789abcdef0123456789";
    private const string NewSecret = "new-secret-0123456789abcdef0123456789";
    private static readonly byte[] Body = Encoding.UTF8.GetBytes("""{"orderId":"A-1001","status":"paid"}""");

    [Fact]
    public void Accepts_both_secrets_during_overlap()
    {
        Assert.Equal(WebhookSecretMatch.Primary,
            WebhookSignature.Verify(WebhookSignature.Sign(NewSecret, Body), Body, NewSecret, OldSecret));
        Assert.Equal(WebhookSecretMatch.Secondary,
            WebhookSignature.Verify(WebhookSignature.Sign(OldSecret, Body), Body, NewSecret, OldSecret));
    }

    [Fact]
    public void Rejects_old_secret_after_overlap_ends()
    {
        Assert.Equal(WebhookSecretMatch.None,
            WebhookSignature.Verify(WebhookSignature.Sign(OldSecret, Body), Body, NewSecret, null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("md5=abc")]
    public void Rejects_missing_or_malformed_header(string? header)
    {
        Assert.Equal(WebhookSecretMatch.None, WebhookSignature.Verify(header, Body, NewSecret, OldSecret));
    }

    [Fact]
    public void Rejects_tampered_body()
    {
        var signature = WebhookSignature.Sign(NewSecret, Body);
        var tampered = Encoding.UTF8.GetBytes("""{"orderId":"A-1001","status":"refunded"}""");

        Assert.Equal(WebhookSecretMatch.None, WebhookSignature.Verify(signature, tampered, NewSecret, null));
    }
}
