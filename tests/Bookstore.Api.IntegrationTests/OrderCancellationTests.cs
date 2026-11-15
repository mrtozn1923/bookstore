using System.Net;
using System.Net.Http.Json;
using Bookstore.Api.Data;
using Bookstore.Api.Endpoints;
using Bookstore.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Bookstore.Api.IntegrationTests;

public sealed class OrderCancellationTests(BookstoreApiFactory factory) : IClassFixture<BookstoreApiFactory>, IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly FakeTimeProvider clock = new(new DateTimeOffset(2026, 11, 14, 9, 0, 0, TimeSpan.Zero));
    private WebApplicationFactory<Program> app = null!;
    private int orderId;
    private int customerId;

    public async ValueTask InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
        // Sahte saat yalnız bu class'ın host'unda; resilience pipeline'ı da bu saati kullanır
        app = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(clock)));

        using IServiceScope scope = app.Services.CreateScope();
        BookstoreDbContext db = scope.ServiceProvider.GetRequiredService<BookstoreDbContext>();
        Customer customer = new() { Email = $"grace-{Guid.NewGuid():N}@example.com", Name = "Grace" };
        db.Customers.Add(customer);
        await db.SaveChangesAsync(Ct);
        customerId = customer.Id;

        Order order = new()
        {
            Reference = "ORD-CANCEL-TEST",
            CustomerId = customer.Id,
            CreatedAt = clock.GetUtcNow(),
            Status = OrderStatus.Placed,
            Total = 54.90m
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync(Ct);
        orderId = order.Id;
    }

    public async ValueTask DisposeAsync() => await app.DisposeAsync();

    private Task<HttpResponseMessage> CancelAsync()
    {
        HttpClient client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestUser.Customer(customerId).HeaderValue);
        return client.PostAsync($"/api/orders/{orderId}/cancel", null, Ct);
    }

    [Fact]
    public async Task Order_can_be_cancelled_within_the_window()
    {
        clock.Advance(TimeSpan.FromMinutes(29));

        HttpResponseMessage response = await CancelAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(OrderStatus.Cancelled, (await response.Content.ReadFromJsonAsync<OrderDto>(Ct))!.Status);
    }

    [Fact]
    public async Task Order_cannot_be_cancelled_after_the_window()
    {
        clock.Advance(TimeSpan.FromMinutes(31));

        HttpResponseMessage response = await CancelAsync();

        ProblemDetails problem = await response.AssertProblemAsync(HttpStatusCode.Conflict);
        Assert.Equal("Cancellation window closed", problem.Title);
    }
}
