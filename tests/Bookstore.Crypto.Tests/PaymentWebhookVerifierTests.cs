using System.Text;
using Bookstore.Crypto.Webhooks;

namespace Bookstore.Crypto.Tests;

public class PaymentWebhookVerifierTests
{
    private static readonly byte[] Current = Encoding.ASCII.GetBytes("test-only-webhook-secret-current");
    private static readonly byte[] Previous = Encoding.ASCII.GetBytes("test-only-webhook-secret-previous");
    private static readonly byte[] Body = """{"orderId":"0197f001-0000-7000-8000-000000000042","amount":450.00}"""u8.ToArray();
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 9, 30, 0, TimeSpan.Zero);

    private static PaymentWebhookVerifier Verifier(params byte[][] secrets) =>
        new(secrets, new FixedTime(Now), TimeSpan.FromMinutes(5));

    private static string Header(byte[] secret, DateTimeOffset at, byte[]? body = null) =>
        PaymentWebhookSignature.CreateHeader(secret, at.ToUnixTimeSeconds(), body ?? Body);

    [Fact]
    public void Untouched_body_is_valid() =>
        Assert.Equal(WebhookVerdict.Valid, Verifier(Current).Verify(Header(Current, Now), Body));

    [Fact]
    public void A_single_changed_byte_breaks_the_signature()
    {
        byte[] tampered = Body.ToArray();
        tampered[^3] = (byte)'1';
        Assert.Equal(WebhookVerdict.BadSignature, Verifier(Current).Verify(Header(Current, Now), tampered));
    }

    [Fact]
    public void Re_serialized_json_is_not_the_signed_body()
    {
        // Same data, different bytes: a space after the colon
        byte[] reformatted = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(Body).Replace("\"amount\":", "\"amount\": "));
        Assert.Equal(WebhookVerdict.BadSignature, Verifier(Current).Verify(Header(Current, Now), reformatted));
    }

    [Theory]
    [InlineData(-6)]
    [InlineData(6)]
    public void Timestamps_outside_the_window_are_rejected(int minutes) =>
        Assert.Equal(WebhookVerdict.StaleTimestamp,
            Verifier(Current).Verify(Header(Current, Now.AddMinutes(minutes)), Body));

    [Fact]
    public void Previous_secret_is_accepted_only_while_it_is_configured()
    {
        string header = Header(Previous, Now);
        Assert.Equal(WebhookVerdict.Valid, Verifier(Current, Previous).Verify(header, Body));
        Assert.Equal(WebhookVerdict.BadSignature, Verifier(Current).Verify(header, Body));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("v1=00")]
    [InlineData("t=abc,v1=50f7edb849549930fcea2bd4d83b1d251cb61d3854f4cb5d375f03d7a60a8ce4")]
    [InlineData("t=1794648600,v1=50f7edb849549930fcea2bd4d83b1d25")]
    public void Malformed_headers_are_rejected(string? header) =>
        Assert.Equal(WebhookVerdict.MalformedHeader, Verifier(Current).Verify(header, Body));

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
