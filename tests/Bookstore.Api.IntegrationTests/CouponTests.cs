using System.Net.Http.Json;
using System.Text.Json;
using Bookstore.Api.Data;
using Bookstore.Api.Endpoints;
using Bookstore.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WireMock.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Bookstore.Api.IntegrationTests;

// Kupon kuralının kombinasyonları burada; E2E yalnız mutlu yolu bir kez dener
public sealed class CouponTests(BookstoreApiFactory factory) : IClassFixture<BookstoreApiFactory>, IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private int customerId;
    private int bookId;

    public async ValueTask InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
        using IServiceScope scope = factory.Services.CreateScope();
        BookstoreDbContext db = scope.ServiceProvider.GetRequiredService<BookstoreDbContext>();
        Customer customer = new() { Email = $"grace-{Guid.NewGuid():N}@example.com", Name = "Grace" };
        Book book = new() { Isbn = "978-0441172719", Title = "Dune", Price = 64.90m, Stock = 5 };
        db.AddRange(customer, book,
            new Coupon { Code = "SAVE10", PercentOff = 10, ValidUntil = DateTimeOffset.UtcNow.AddDays(7) },
            new Coupon { Code = "SPRING10", PercentOff = 10, ValidUntil = DateTimeOffset.UtcNow.AddDays(-1) });
        await db.SaveChangesAsync(Ct);
        (customerId, bookId) = (customer.Id, book.Id);

        factory.PaymentApi
            .Given(Request.Create().WithPath("/v1/charges").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new { chargeId = "ch_1", status = "succeeded" }));
        factory.ShippingApi
            .Given(Request.Create().WithPath("/v1/shipments").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(201).WithBodyAsJson(new { trackingNumber = "TRK-1" }));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private Task<HttpResponseMessage> PlaceOrderAsync(string? coupon) =>
        factory.CreateClientFor(TestUser.Customer(customerId))
            .PostAsJsonAsync("/api/orders", new PlaceOrderRequest([new OrderLineRequest(bookId, 1)], "Izmir", coupon), Ct);

    [Fact]
    public async Task Valid_coupon_reduces_the_charged_total()
    {
        HttpResponseMessage response = await PlaceOrderAsync("SAVE10");

        response.AssertCreatedAt("/api/orders/");
        OrderDto order = (await response.Content.ReadFromJsonAsync<OrderDto>(Ct))!;
        Assert.Equal(58.41m, order.Total);
        ILogEntry charge = Assert.Single(factory.PaymentApi.LogEntries);
        Assert.Equal(58.41m, JsonDocument.Parse(charge.RequestMessage!.Body!).RootElement.GetProperty("amount").GetDecimal());
    }

    [Theory]
    [InlineData("SPRING10")]
    [InlineData("NOPE")]
    public async Task Expired_or_unknown_coupon_is_rejected_without_charging(string coupon)
    {
        HttpResponseMessage response = await PlaceOrderAsync(coupon);

        HttpValidationProblemDetails problem = await response.AssertValidationProblemAsync();
        Assert.Equal($"Coupon {coupon} is not valid.", Assert.Single(problem.Errors["couponCode"]));
        Assert.Empty(factory.PaymentApi.LogEntries);
    }
}
