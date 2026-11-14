using System.Security.Cryptography;
using System.Text;
using Bookstore.Contracts.Payments;
using Bookstore.Payments.Data;
using Bookstore.Payments.Webhooks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Time.Testing;

namespace Bookstore.Payments.Tests;

public sealed class WebhookSignatureTests
{
    // A test-only secret: "whsec_" + base64 of 24 fixed bytes.
    private static readonly byte[] Key = [.. Enumerable.Range(1, 24).Select(i => (byte)i)];
    private static readonly string Secret = "whsec_" + Convert.ToBase64String(Key);
    private static readonly FakeTimeProvider Clock = new(new DateTimeOffset(2026, 11, 14, 12, 0, 0, TimeSpan.Zero));
    private const string Body = """{"type":"charge.succeeded","data":{"id":"ch_1"}}""";

    private readonly WebhookSignature signature = new(
        new ConfigurationBuilder().AddInMemoryCollection([new("PaymentProvider:WebhookSecret", Secret)]).Build(), Clock);

    private static HeaderDictionary Headers(string id, DateTimeOffset at, string signatureHeader) => new()
    {
        ["webhook-id"] = id,
        ["webhook-timestamp"] = at.ToUnixTimeSeconds().ToString(),
        ["webhook-signature"] = signatureHeader
    };

    private static string Sign(string id, DateTimeOffset at, string body) =>
        "v1," + Convert.ToBase64String(HMACSHA256.HashData(Key, Encoding.UTF8.GetBytes($"{id}.{at.ToUnixTimeSeconds()}.{body}")));

    [Fact]
    public void A_correctly_signed_webhook_is_accepted()
    {
        Assert.True(signature.IsValid(Headers("evt_1", Clock.GetUtcNow(), Sign("evt_1", Clock.GetUtcNow(), Body)), Body, out _));
    }

    [Fact]
    public void A_changed_body_is_rejected()
    {
        string header = Sign("evt_1", Clock.GetUtcNow(), Body);

        Assert.False(signature.IsValid(Headers("evt_1", Clock.GetUtcNow(), header), Body.Replace("succeeded", "failed"), out string error));
        Assert.Equal("signature mismatch", error);
    }

    [Fact]
    public void A_replayed_old_webhook_is_rejected_even_with_a_valid_signature()
    {
        DateTimeOffset sixMinutesAgo = Clock.GetUtcNow().AddMinutes(-6);

        Assert.False(signature.IsValid(Headers("evt_1", sixMinutesAgo, Sign("evt_1", sixMinutesAgo, Body)), Body, out string error));
        Assert.Equal("timestamp outside tolerance", error);
    }

    [Fact]
    public void During_secret_rotation_any_listed_signature_may_match()
    {
        string header = "v1,AAAA " + Sign("evt_1", Clock.GetUtcNow(), Body);

        Assert.True(signature.IsValid(Headers("evt_1", Clock.GetUtcNow(), header), Body, out _));
    }

    [Fact]
    public void A_payment_produces_its_event_only_once()
    {
        Payment payment = Payment.Start(new AuthorizePayment(Guid.CreateVersion7(), 249.90m, "TRY", "ada@example.com"), Clock);

        object? first = payment.Complete(succeeded: true, "ch_1", null, Clock);
        object? second = payment.Complete(succeeded: true, "ch_1", null, Clock);

        Assert.IsType<PaymentSucceeded>(first);
        Assert.Null(second);
    }
}
