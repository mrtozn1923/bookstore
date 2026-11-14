using System.Net;
using System.Net.Http.Json;
using Bookstore.FakeProviders;
using Microsoft.Extensions.Time.Testing;
using WireMock;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Bookstore.Api.Tests;

public sealed class CheckoutResilienceTests : IDisposable
{
    private readonly WireMockServer _payments = FakePaymentProvider.Start();
    private readonly WireMockServer _carrier = FakeLegacyCarrier.Start();

    public void Dispose()
    {
        _payments.Dispose();
        _carrier.Dispose();
    }

    private int QuoteRequests => _carrier.LogEntries.Count(e => e.RequestMessage?.Path == "/api/v2/quotes");

    private int ChargeRequests => _payments.LogEntries.Count(e => e.RequestMessage?.Path == "/v1/charges");

    private static Task<HttpResponseMessage> CheckoutAsync(HttpClient client) =>
        client.PostAsJsonAsync("/api/orders/1001/checkout", new { cardToken = "tok_visa" }, TestContext.Current.CancellationToken);

    [Fact]
    public async Task Carrier_errors_are_retried_then_checkout_uses_the_flat_tariff()
    {
        FakeLegacyCarrier.Configure(_carrier, FakeMode.Error);
        await using var api = new BookstoreApiFactory(_payments, _carrier);

        using var response = await CheckoutAsync(api.CreateClient());

        var result = await response.Content.ReadFromJsonAsync<CheckoutResponse>(TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(result!.ShippingIsFlatRate);
        Assert.Equal(3, QuoteRequests); // 1 attempt + 2 retries
    }

    [Fact]
    public async Task Payment_post_is_never_retried()
    {
        FakePaymentProvider.Configure(_payments, FakeMode.Error);
        await using var api = new BookstoreApiFactory(_payments, _carrier);

        using var response = await CheckoutAsync(api.CreateClient());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(1, ChargeRequests);
    }

    [Fact]
    public async Task Circuit_opens_and_stops_calling_the_carrier()
    {
        FakeLegacyCarrier.Configure(_carrier, FakeMode.Error);
        await using var api = new BookstoreApiFactory(_payments, _carrier);
        var client = api.CreateClient();

        for (var i = 0; i < 4; i++)
        {
            using var _ = await CheckoutAsync(client);
        }

        var before = QuoteRequests;
        using var next = await CheckoutAsync(client);
        var health = await client.GetStringAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(before, QuoteRequests); // the open circuit short-circuits the call
        Assert.Equal("Degraded", health);
    }

    [Fact]
    public async Task Retry_After_backoff_runs_on_fake_time()
    {
        // 429 + Retry-After: 2 twice, then the normal quote.
        _carrier.Given(Request.Create().WithPath("/api/v2/quotes").UsingGet())
            .InScenario("throttle").WillSetStateTo("once")
            .AtPriority(-1) // before the fake's own quote mapping
            .RespondWith(Response.Create().WithStatusCode(429).WithHeader("Retry-After", "2"));
        _carrier.Given(Request.Create().WithPath("/api/v2/quotes").UsingGet())
            .InScenario("throttle").WhenStateIs("once").WillSetStateTo("twice")
            .AtPriority(-1) // before the fake's own quote mapping
            .RespondWith(Response.Create().WithStatusCode(429).WithHeader("Retry-After", "2"));

        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        await using var api = new BookstoreApiFactory(_payments, _carrier, time);
        var client = api.CreateClient();
        var started = TimeProvider.System.GetTimestamp();

        var checkout = CheckoutAsync(client);
        while (!checkout.IsCompleted)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(20), TestContext.Current.CancellationToken);
            time.Advance(TimeSpan.FromMilliseconds(500));
        }

        using var response = await checkout;
        var result = await response.Content.ReadFromJsonAsync<CheckoutResponse>(TestContext.Current.CancellationToken);
        var realTime = TimeProvider.System.GetElapsedTime(started);
        TestContext.Current.TestOutputHelper?.WriteLine($"quote requests={QuoteRequests}, flat rate={result?.ShippingIsFlatRate}, real time={realTime.TotalMilliseconds:0} ms");

        Assert.False(result!.ShippingIsFlatRate);
        Assert.Equal(3, QuoteRequests);
        Assert.True(realTime < TimeSpan.FromSeconds(2), $"took {realTime}");
    }

    [Fact]
    public async Task Checkout_with_twenty_percent_payment_failures()
    {
        var random = new Random(42);
        var failures = 0;
        _payments.Reset();
        _payments.Given(Request.Create().WithPath("/v1/charges").UsingPost())
            .RespondWith(Response.Create().WithCallback(_ =>
            {
                lock (random)
                {
                    if (random.NextDouble() < 0.2)
                    {
                        failures++;
                        return new ResponseMessage { StatusCode = 500 };
                    }
                }

                var ok = new ResponseMessage { StatusCode = 201 };
                ok.AddHeader("Content-Type", "application/json");
                ok.BodyData = new WireMock.Util.BodyData
                {
                    DetectedBodyType = WireMock.Types.BodyType.String,
                    BodyAsString = """{"id":"ch_3MtwBwLkdIwHu7ix","status":"succeeded","amount":54890,"currency":"TRY"}"""
                };
                return ok;
            }));
        await using var api = new BookstoreApiFactory(_payments, _carrier);
        var client = api.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 100; i++)
        {
            using var response = await CheckoutAsync(client);
            statuses.Add(response.StatusCode);
        }

        var summary = string.Join(", ", statuses.GroupBy(s => s).Select(g => $"{(int)g.Key} x {g.Count()}"));
        TestContext.Current.TestOutputHelper?.WriteLine($"responses: {summary}; provider 500s: {failures}; charge requests: {ChargeRequests}");

        Assert.DoesNotContain(HttpStatusCode.InternalServerError, statuses);
        Assert.Equal(failures, statuses.Count(s => s == HttpStatusCode.ServiceUnavailable));
        Assert.Equal(100, ChargeRequests); // no POST retries, even under failures
    }

    [Fact]
    public async Task Chaos_faults_on_the_carrier_never_reach_the_network()
    {
        await using var api = new BookstoreApiFactory(_payments, _carrier, settings: new Dictionary<string, string?>
        {
            ["Chaos:LegacyCarrier:Enabled"] = "true",
            ["Chaos:LegacyCarrier:FaultRate"] = "1.0"
        });

        using var response = await CheckoutAsync(api.CreateClient());
        var result = await response.Content.ReadFromJsonAsync<CheckoutResponse>(TestContext.Current.CancellationToken);

        Assert.True(result!.ShippingIsFlatRate);
        Assert.Equal(0, QuoteRequests);
    }

    private sealed record CheckoutResponse(int OrderId, string Status, decimal Total, bool ShippingIsFlatRate);
}
