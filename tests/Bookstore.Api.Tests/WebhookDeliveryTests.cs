using System.Net.Http.Json;
using Bookstore.FakeProviders;
using Bookstore.Webhooks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Bookstore.Api.Tests;

public sealed class WebhookDeliveryTests : IDisposable
{
    private readonly WireMockServer _payments = FakePaymentProvider.Start();
    private readonly WireMockServer _carrier = FakeLegacyCarrier.Start();
    private readonly WireMockServer _receiver = FakeWebhookReceiver.Start();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 14, 9, 0, 0, TimeSpan.Zero));

    public void Dispose()
    {
        _payments.Dispose();
        _carrier.Dispose();
        _receiver.Dispose();
    }

    // The background loop never ticks in these tests; each delivery is triggered by hand.
    private BookstoreApiFactory CreateApi() => new(_payments, _carrier, _time, new Dictionary<string, string?>
    {
        ["Webhooks:Delivery:PollInterval"] = "1.00:00:00"
    });

    private async Task<WebhookMessage> ShipInstitutionalOrderAsync(BookstoreApiFactory api)
    {
        var client = api.CreateClient();
        var ct = TestContext.Current.CancellationToken;
        using var subscribe = await client.PostAsJsonAsync("/admin/webhooks/subscriptions",
            new { institutionId = "school-0042", url = _receiver.Url + FakeWebhookReceiver.Path, eventTypes = new[] { "order.shipped" } }, ct);
        subscribe.EnsureSuccessStatusCode();
        using var checkout = await client.PostAsJsonAsync("/api/orders/1003/checkout", new { cardToken = "tok_visa" }, ct);
        using var ship = await client.PostAsJsonAsync("/api/orders/1003/ship", new { trackingNumber = "LC-7781240" }, ct);
        ship.EnsureSuccessStatusCode();
        return Assert.Single(api.Services.GetRequiredService<WebhookOutbox>().All());
    }

    private static WebhookDispatcher Dispatcher(BookstoreApiFactory api) =>
        api.Services.GetServices<IHostedService>().OfType<WebhookDispatcher>().Single();

    [Fact]
    public async Task Failed_deliveries_follow_the_schedule_with_the_same_webhook_id()
    {
        // 500, 500, then 200
        _receiver.Reset();
        _receiver.Given(Request.Create().WithPath(FakeWebhookReceiver.Path).UsingPost())
            .InScenario("flaky").WillSetStateTo("once")
            .RespondWith(Response.Create().WithStatusCode(500));
        _receiver.Given(Request.Create().WithPath(FakeWebhookReceiver.Path).UsingPost())
            .InScenario("flaky").WhenStateIs("once").WillSetStateTo("twice")
            .RespondWith(Response.Create().WithStatusCode(500));
        _receiver.Given(Request.Create().WithPath(FakeWebhookReceiver.Path).UsingPost())
            .InScenario("flaky").WhenStateIs("twice")
            .RespondWith(Response.Create().WithStatusCode(200));
        await using var api = CreateApi();
        var message = await ShipInstitutionalOrderAsync(api);
        var dispatcher = Dispatcher(api);
        var ct = TestContext.Current.CancellationToken;

        var delays = new List<TimeSpan>();
        while (message.Status == WebhookMessageStatus.Pending)
        {
            _time.SetUtcNow(message.NextAttemptAt);
            await dispatcher.DeliverAsync(message, ct);
            if (message.Status == WebhookMessageStatus.Pending)
            {
                delays.Add(message.NextAttemptAt - _time.GetUtcNow());
            }
        }

        var requests = _receiver.LogEntries.Select(e => e.RequestMessage!).ToList();
        var ids = requests.Select(r => r.Headers!["webhook-id"].ToString()).Distinct().ToList();
        var timestamps = requests.Select(r => r.Headers!["webhook-timestamp"].ToString()).ToList();
        TestContext.Current.TestOutputHelper?.WriteLine(
            $"status={message.Status}, requests={requests.Count}, webhook-ids={ids.Count}, timestamps={string.Join(",", timestamps)}, delays={string.Join(", ", delays)}");

        Assert.Equal(WebhookMessageStatus.Delivered, message.Status);
        Assert.Equal(3, requests.Count);
        Assert.Single(ids);
        Assert.Equal(3, timestamps.Distinct().Count()); // a new timestamp, and signature, per attempt
        Assert.InRange(delays[0], TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5.5));
        Assert.InRange(delays[1], TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5.5));
    }

    [Fact]
    public async Task Gone_disables_the_subscription()
    {
        FakeWebhookReceiver.Configure(_receiver, FakeMode.Gone);
        await using var api = CreateApi();
        var message = await ShipInstitutionalOrderAsync(api);

        await Dispatcher(api).DeliverAsync(message, TestContext.Current.CancellationToken);

        var subscription = api.Services.GetRequiredService<WebhookSubscriptionStore>().Find(message.SubscriptionId)!;
        Assert.Equal(WebhookMessageStatus.Failed, message.Status);
        Assert.Equal(WebhookSubscriptionStatus.Disabled, subscription.Status);
    }
}
