using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bookstore.Api.Data;
using Bookstore.Api.Endpoints;
using Bookstore.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WireMock.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Bookstore.Api.IntegrationTests;

public sealed class OrderPlacementTests(BookstoreApiFactory factory, MailpitFixture mail) : IClassFixture<BookstoreApiFactory>, IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly string customerEmail = $"ada-{Guid.NewGuid():N}@example.com";
    private int customerId;
    private int bookId;

    public async ValueTask InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
        using IServiceScope scope = factory.Services.CreateScope();
        BookstoreDbContext db = scope.ServiceProvider.GetRequiredService<BookstoreDbContext>();
        Customer customer = new() { Email = customerEmail, Name = "Ada" };
        Book book = new() { Isbn = "978-0132350884", Title = "Clean Code", Price = 54.90m, Stock = 3 };
        db.AddRange(customer, book);
        await db.SaveChangesAsync(Ct);
        (customerId, bookId) = (customer.Id, book.Id);

        factory.ShippingApi
            .Given(Request.Create().WithPath("/v1/shipments").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(201).WithBodyAsJson(new { trackingNumber = "TRK-1001" }));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private Task<HttpResponseMessage> PlaceOrderAsync(int quantity = 2) =>
        factory.CreateClientFor(TestUser.Customer(customerId))
            .PostAsJsonAsync("/api/orders", new PlaceOrderRequest([new OrderLineRequest(bookId, quantity)], "Izmir"), Ct);

    private void PaymentApproves() =>
        factory.PaymentApi
            .Given(Request.Create().WithPath("/v1/charges").UsingPost().WithHeader("X-Api-Key", "test-key"))
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new { chargeId = "ch_1001", status = "succeeded" }));

    private async Task<int> StockAsync()
    {
        using IServiceScope scope = factory.Services.CreateScope();
        BookstoreDbContext db = scope.ServiceProvider.GetRequiredService<BookstoreDbContext>();
        return await db.Books.Where(b => b.Id == bookId).Select(b => b.Stock).SingleAsync(Ct);
    }

    [Fact]
    public async Task Order_charges_payment_ships_and_sends_confirmation()
    {
        PaymentApproves();

        HttpResponseMessage response = await PlaceOrderAsync();

        response.AssertCreatedAt("/api/orders/");
        OrderDto order = (await response.Content.ReadFromJsonAsync<OrderDto>(Ct))!;
        Assert.Equal("TRK-1001", order.TrackingNumber);

        ILogEntry charge = Assert.Single(factory.PaymentApi.LogEntries);
        JsonElement body = JsonDocument.Parse(charge.RequestMessage.Body!).RootElement;
        Assert.Equal(109.80m, body.GetProperty("amount").GetDecimal());
        Assert.Equal("TRY", body.GetProperty("currency").GetString());
        Assert.Equal(order.Reference, body.GetProperty("orderReference").GetString());

        Assert.Equal(1, await StockAsync());

        MailpitMessage email = Assert.Single(await mail.MessagesToAsync(customerEmail));
        Assert.Equal($"Order {order.Reference} confirmed", email.Subject);
    }

    [Fact]
    public async Task Declined_payment_returns_422_and_keeps_stock()
    {
        factory.PaymentApi
            .Given(Request.Create().WithPath("/v1/charges").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(402).WithBodyAsJson(new { error = "card_declined" }));

        HttpResponseMessage response = await PlaceOrderAsync();

        ProblemDetails problem = await response.AssertProblemAsync(HttpStatusCode.UnprocessableEntity);
        Assert.Equal("Payment declined", problem.Title);
        Assert.Equal(3, await StockAsync());
        Assert.Empty(await mail.MessagesToAsync(customerEmail));
    }

    [Fact]
    public async Task Two_transient_failures_are_retried_and_the_order_succeeds()
    {
        factory.PaymentApi
            .Given(Request.Create().WithPath("/v1/charges").UsingPost())
            .InScenario("flaky-payments").WillSetStateTo("failed-once")
            .RespondWith(Response.Create().WithStatusCode(500));
        factory.PaymentApi
            .Given(Request.Create().WithPath("/v1/charges").UsingPost())
            .InScenario("flaky-payments").WhenStateIs("failed-once").WillSetStateTo("failed-twice")
            .RespondWith(Response.Create().WithStatusCode(503));
        factory.PaymentApi
            .Given(Request.Create().WithPath("/v1/charges").UsingPost())
            .InScenario("flaky-payments").WhenStateIs("failed-twice")
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new { chargeId = "ch_1002", status = "succeeded" }));

        HttpResponseMessage response = await PlaceOrderAsync();

        response.AssertCreatedAt("/api/orders/");
        Assert.Equal([500, 503, 200], factory.PaymentApi.LogEntries.Select(e => (int)e.ResponseMessage.StatusCode!));
    }

    [Fact]
    public async Task Persistent_payment_failure_returns_503_after_retries()
    {
        factory.PaymentApi
            .Given(Request.Create().WithPath("/v1/charges").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(500));

        HttpResponseMessage response = await PlaceOrderAsync();

        ProblemDetails problem = await response.AssertProblemAsync(HttpStatusCode.ServiceUnavailable);
        Assert.Equal("Payment provider unavailable", problem.Title);
        Assert.Equal(4, factory.PaymentApi.LogEntries.Count());
        Assert.Equal(3, await StockAsync());
    }

    [Fact]
    public async Task Slow_payment_provider_times_out_each_attempt()
    {
        factory.PaymentApi
            .Given(Request.Create().WithPath("/v1/charges").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithDelay(TimeSpan.FromSeconds(2))
                .WithBodyAsJson(new { chargeId = "ch_late", status = "succeeded" }));

        Stopwatch stopwatch = Stopwatch.StartNew();
        HttpResponseMessage response = await PlaceOrderAsync();
        stopwatch.Stop();

        await response.AssertProblemAsync(HttpStatusCode.ServiceUnavailable);
        Assert.InRange(stopwatch.Elapsed, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4));
        // WireMock bir isteği cevabı bittiğinde log'a yazar; geciken cevapları bekle
        await WaitForRequestsAsync(factory.PaymentApi, expected: 4);
    }

    private static async Task WaitForRequestsAsync(WireMockServer server, int expected)
    {
        for (int i = 0; i < 50 && server.LogEntries.Count() < expected; i++)
        {
            await Task.Delay(100, Ct);
        }
        Assert.Equal(expected, server.LogEntries.Count());
    }

    [Fact]
    public async Task Shipping_failure_does_not_fail_the_order()
    {
        PaymentApproves();
        factory.ShippingApi.Reset();
        factory.ShippingApi
            .Given(Request.Create().WithPath("/v1/shipments").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(500));

        HttpResponseMessage response = await PlaceOrderAsync();

        response.AssertCreatedAt("/api/orders/");
        OrderDto order = (await response.Content.ReadFromJsonAsync<OrderDto>(Ct))!;
        Assert.Null(order.TrackingNumber);
    }
}
