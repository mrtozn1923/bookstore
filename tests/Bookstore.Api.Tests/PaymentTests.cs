using System.Net;
using System.Net.Http.Json;
using Bookstore.Ordering;
using Bookstore.Payments;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Bookstore.Api.Tests;

public sealed class PaymentTests(BookstoreApiFactory factory)
{
    private const string Emma = "9780141439587";
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(ChargeOutcome.Succeeded, "Paid")]
    [InlineData(ChargeOutcome.Failed, "PaymentPending")]
    public async Task Order_status_follows_the_payment_outcome(ChargeOutcome outcome, string expectedStatus)
    {
        using HttpClient client = factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.AddSingleton<IPaymentGateway>(new FixedPaymentGateway(outcome))))
            .CreateClient();

        HttpResponseMessage created = await client.PostAsync("/api/carts", null, ct);
        CartView? cart = await created.Content.ReadFromJsonAsync<CartView>(ct);
        (await client.PutAsJsonAsync($"/api/carts/{cart!.Id}/items/{Emma}", new SetQuantityRequest(1), ct))
            .EnsureSuccessStatusCode();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/orders",
            new PlaceOrderRequest(cart.Id, "Grace Hopper", "grace@example.com"), ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        OrderView? order = await response.Content.ReadFromJsonAsync<OrderView>(ct);
        Assert.Equal(expectedStatus, order?.Status);
    }

    private sealed class FixedPaymentGateway(ChargeOutcome outcome) : IPaymentGateway
    {
        public Task<ChargeOutcome> ChargeAsync(int orderId, decimal amount, string currency, CancellationToken ct) =>
            Task.FromResult(outcome);
    }
}
