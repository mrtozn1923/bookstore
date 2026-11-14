using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bookstore.FakeProviders;
using Bookstore.Ordering;
using Bookstore.Payments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
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

    private IEnumerable<string?> ChargeKeys => _payments.LogEntries
        .Where(e => e.RequestMessage?.Path == "/v1/charges" && e.RequestMessage.Method == "POST")
        .Select(e => e.RequestMessage!.Headers?["Idempotency-Key"]?.ToString());

    private static Task<HttpResponseMessage> CheckoutAsync(HttpClient client, int orderId = 1001) =>
        client.PostAsJsonAsync($"/api/orders/{orderId}/checkout", new { cardToken = "tok_visa" }, TestContext.Current.CancellationToken);

    // A paid order is never charged again, so tests that need many checkouts need many orders.
    private static void AddOrders(BookstoreApiFactory api, int firstId, int count)
    {
        var orders = api.Services.GetRequiredService<OrderStore>();
        for (var id = firstId; id < firstId + count; id++)
        {
            orders.Add(new Order { Id = id, CustomerName = "Grace", CustomerEmail = "grace@example.com", ShipToPostalCode = "06100", ItemsTotal = 120m, WeightGrams = 400 });
        }
    }

    // Short retry delays so tests do not wait for the production backoff.
    private static readonly Dictionary<string, string?> FastPaymentRetries = new()
    {
        ["Resilience:PaymentProvider:Retry:Delay"] = "00:00:00.010"
    };

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
    public async Task Payment_retries_reuse_one_idempotency_key()
    {
        FakePaymentProvider.Configure(_payments, FakeMode.Error);
        await using var api = new BookstoreApiFactory(_payments, _carrier, settings: FastPaymentRetries);

        using var response = await CheckoutAsync(api.CreateClient());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(4, ChargeRequests); // 1 attempt + 3 retries
        var key = Assert.Single(ChargeKeys.Distinct());
        TestContext.Current.TestOutputHelper?.WriteLine($"charge requests={ChargeRequests}, distinct keys=1, key={key}");
    }

    [Fact]
    public async Task Circuit_opens_and_stops_calling_the_carrier()
    {
        FakeLegacyCarrier.Configure(_carrier, FakeMode.Error);
        await using var api = new BookstoreApiFactory(_payments, _carrier);
        var client = api.CreateClient();
        AddOrders(api, 2001, 5);

        for (var i = 0; i < 4; i++)
        {
            using var _ = await CheckoutAsync(client, 2001 + i);
        }

        var before = QuoteRequests;
        using var next = await CheckoutAsync(client, 2005);
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
        FakePaymentProvider.Configure(_payments, new FakePaymentSettings { FailureRate = 0.2, Seed = 42 });
        await using var api = new BookstoreApiFactory(_payments, _carrier, settings: FastPaymentRetries);
        var client = api.CreateClient();
        AddOrders(api, 2001, 100);

        var statuses = new List<HttpStatusCode>();
        for (var id = 2001; id <= 2100; id++)
        {
            using var response = await CheckoutAsync(client, id);
            statuses.Add(response.StatusCode);
        }

        // Ask the provider how many charges it really created.
        using var provider = new HttpClient { BaseAddress = new Uri(_payments.Url!) };
        var charges = (await provider.GetFromJsonAsync<ChargeList>("/v1/charges", TestContext.Current.CancellationToken))!;
        var summary = string.Join(", ", statuses.GroupBy(s => s).Select(g => $"{(int)g.Key} x {g.Count()}"));
        TestContext.Current.TestOutputHelper?.WriteLine(
            $"responses: {summary}; charge requests: {ChargeRequests}; distinct keys: {ChargeKeys.Distinct().Count()}; charges at provider: {charges.Data.Count}");

        Assert.DoesNotContain(HttpStatusCode.InternalServerError, statuses);
        Assert.Equal(statuses.Count(s => s == HttpStatusCode.OK), charges.Data.Count); // one charge per paid order
        Assert.True(ChargeRequests > ChargeKeys.Distinct().Count()); // retries happened, always with the same key
    }

    [Fact]
    public async Task Unknown_outcome_is_settled_by_reconciliation()
    {
        // The provider needs 1 s, we give up after 0.5 s: the charge happens, the answer is lost.
        FakePaymentProvider.Configure(_payments, new FakePaymentSettings { Mode = FakeMode.Slow, Delay = TimeSpan.FromSeconds(1) });
        await using var api = new BookstoreApiFactory(_payments, _carrier, settings: new Dictionary<string, string?>
        {
            ["Resilience:PaymentProvider:Retry:Delay"] = "00:00:00.010",
            ["Resilience:PaymentProvider:AttemptTimeout:Timeout"] = "00:00:00.200",
            ["Resilience:PaymentProvider:TotalRequestTimeout:Timeout"] = "00:00:00.500",
            ["Payments:Reconciliation:MinAge"] = "00:00:00"
        });
        var client = api.CreateClient();

        using var checkout = await CheckoutAsync(client);
        Assert.Equal(HttpStatusCode.Accepted, checkout.StatusCode);

        await Task.Delay(TimeSpan.FromSeconds(1.5), TestContext.Current.CancellationToken);
        var job = api.Services.GetServices<IHostedService>().OfType<PaymentReconciliationJob>().Single();
        await job.RunOnceAsync(TestContext.Current.CancellationToken);

        var order = api.Services.GetRequiredService<OrderStore>().Find(1001)!;
        TestContext.Current.TestOutputHelper?.WriteLine($"checkout={(int)checkout.StatusCode}, after reconciliation: {order.Status} {order.ProviderPaymentId}, charge requests={ChargeRequests}");
        Assert.Equal(OrderStatus.Paid, order.Status);
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

    private sealed record ChargeList(List<JsonElement> Data);
}
