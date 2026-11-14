using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Bookstore.FakeProviders;
using Bookstore.Ordering;
using Bookstore.Payments;
using Bookstore.Payments.Webhooks;
using Bookstore.Webhooks;
using Microsoft.Extensions.DependencyInjection;
using WireMock.Server;

namespace Bookstore.Api.Tests;

public sealed class PaymentWebhookTests : IDisposable
{
    private const string OldSecret = "whsec_Ym9va3N0b3JlLWZha2Utd2ViaG9vay1zZWNyZXQtMDAwMA==";
    private const string NewSecret = "whsec_Ym9va3N0b3JlLWZha2Utd2ViaG9vay1zZWNyZXQtMDAwMQ==";

    private readonly WireMockServer _payments = FakePaymentProvider.Start(null,
        new FakePaymentSettings { Mode = FakeMode.Slow, Delay = TimeSpan.FromSeconds(1) });
    private readonly WireMockServer _carrier = FakeLegacyCarrier.Start();

    public void Dispose()
    {
        _payments.Dispose();
        _carrier.Dispose();
    }

    // The provider needs 1 s, checkout gives up after 0.5 s: the order is left with an open attempt.
    private BookstoreApiFactory CreateApi(params string[] secrets)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Resilience:PaymentProvider:Retry:Delay"] = "00:00:00.010",
            ["Resilience:PaymentProvider:AttemptTimeout:Timeout"] = "00:00:00.200",
            ["Resilience:PaymentProvider:TotalRequestTimeout:Timeout"] = "00:00:00.500"
        };
        for (var i = 0; i < secrets.Length; i++)
        {
            settings[$"Payments:Provider:Webhooks:Secrets:{i}"] = secrets[i];
        }

        return new BookstoreApiFactory(_payments, _carrier, settings: settings);
    }

    private static async Task<PaymentAttempt> LeaveOpenAttemptAsync(BookstoreApiFactory api, HttpClient client)
    {
        using var checkout = await client.PostAsJsonAsync("/api/orders/1001/checkout", new { cardToken = "tok_visa" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Accepted, checkout.StatusCode);
        await Task.Delay(TimeSpan.FromSeconds(1.5), TestContext.Current.CancellationToken); // the charge now exists at the provider
        return api.Services.GetRequiredService<PaymentAttemptStore>().ForOrder(1001).Single();
    }

    // The provider's format: spaces after ':' and ',' and a "+00:00" offset.
    private static string ProviderBody(PaymentAttempt attempt) =>
        $$$"""{"type": "payment.succeeded", "timestamp": "2026-11-14T10:00:00.000000+00:00", "data": {"id": "ch_000001", "status": "succeeded", "amount": {{{(long)(attempt.Amount * 100)}}}, "currency": "TRY", "merchant_reference": "{{{attempt.Reference}}}"}}""";

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, string id, string body, DateTimeOffset sentAt, string secret, string? signedBody = null)
    {
        var timestamp = sentAt.ToUnixTimeSeconds();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/payment-provider")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("webhook-id", id);
        request.Headers.Add("webhook-timestamp", timestamp.ToString());
        request.Headers.Add("webhook-signature", WebhookSignature.Sign(id, timestamp, Encoding.UTF8.GetBytes(signedBody ?? body), [secret]));
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Signed_webhook_is_acknowledged_fast_and_processed_once()
    {
        await using var api = CreateApi(OldSecret);
        var client = api.CreateClient();
        var attempt = await LeaveOpenAttemptAsync(api, client);
        var body = ProviderBody(attempt);

        var started = Stopwatch.GetTimestamp();
        using var first = await SendAsync(client, "msg_test_0001", body, DateTimeOffset.UtcNow, OldSecret);
        var elapsed = Stopwatch.GetElapsedTime(started);
        using var copy = await SendAsync(client, "msg_test_0001", body, DateTimeOffset.UtcNow, OldSecret);

        var inbox = api.Services.GetRequiredService<PaymentWebhookInbox>();
        for (var i = 0; i < 50 && inbox.Find("msg_test_0001")!.Status == InboxStatus.Received; i++)
        {
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }

        var order = api.Services.GetRequiredService<OrderStore>().Find(1001)!;
        TestContext.Current.TestOutputHelper?.WriteLine(
            $"first={(int)first.StatusCode} in {elapsed.TotalMilliseconds:0} ms, copy={(int)copy.StatusCode}, inbox={inbox.Find("msg_test_0001")!.Status} attempts={inbox.Find("msg_test_0001")!.Attempts}, order={order.Status}");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, copy.StatusCode);
        Assert.Equal(1, inbox.Find("msg_test_0001")!.Attempts);
        Assert.Equal(OrderStatus.Paid, order.Status);
    }

    [Fact]
    public async Task Re_serialized_body_fails_verification()
    {
        await using var api = CreateApi(OldSecret);
        var client = api.CreateClient();
        var attempt = await LeaveOpenAttemptAsync(api, client);
        var original = ProviderBody(attempt);

        // The classic mistake: bind to a DTO, serialize it again and check the signature against that.
        var dto = JsonSerializer.Deserialize<JsonElement>(original);
        var reserialized = JsonSerializer.Serialize(dto);
        using var response = await SendAsync(client, "msg_test_0002", reserialized, DateTimeOffset.UtcNow, OldSecret, signedBody: original);

        TestContext.Current.TestOutputHelper?.WriteLine($"original:     {original}\nreserialized: {reserialized}\nstatus: {(int)response.StatusCode}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Replayed_webhook_outside_the_tolerance_is_rejected()
    {
        await using var api = CreateApi(OldSecret);
        var client = api.CreateClient();
        var attempt = await LeaveOpenAttemptAsync(api, client);

        using var response = await SendAsync(client, "msg_test_0003", ProviderBody(attempt), DateTimeOffset.UtcNow.AddMinutes(-10), OldSecret);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Previous_secret_is_accepted_during_rotation()
    {
        await using var api = CreateApi(NewSecret, OldSecret);
        var client = api.CreateClient();
        var attempt = await LeaveOpenAttemptAsync(api, client);

        using var response = await SendAsync(client, "msg_test_0004", ProviderBody(attempt), DateTimeOffset.UtcNow, OldSecret);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
