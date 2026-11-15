using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Bookstore.Api.Data;
using Bookstore.Api.Endpoints;
using Bookstore.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Bookstore.Api.IntegrationTests;

public sealed class PaymentCircuitBreakerTests(BookstoreApiFactory factory) : IClassFixture<BookstoreApiFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Open_circuit_fails_fast_without_calling_the_provider()
    {
        await factory.ResetDatabaseAsync();
        // Devre, 4 denemelik pencerede hataların yarısı başarısızsa açılır
        await using WebApplicationFactory<Program> app = factory.WithWebHostBuilder(builder => builder
            .UseSetting("Payments:Resilience:CircuitBreaker:MinimumThroughput", "4")
            .UseSetting("Payments:Resilience:CircuitBreaker:FailureRatio", "0.5")
            .UseSetting("Payments:Resilience:CircuitBreaker:BreakDuration", "00:00:30"));

        int customerId, bookId;
        using (IServiceScope scope = app.Services.CreateScope())
        {
            BookstoreDbContext db = scope.ServiceProvider.GetRequiredService<BookstoreDbContext>();
            Customer customer = new() { Email = $"alan-{Guid.NewGuid():N}@example.com", Name = "Alan" };
            Book book = new() { Isbn = "978-0201633610", Title = "Design Patterns", Price = 80m, Stock = 10 };
            db.AddRange(customer, book);
            await db.SaveChangesAsync(Ct);
            (customerId, bookId) = (customer.Id, book.Id);
        }

        factory.PaymentApi
            .Given(Request.Create().WithPath("/v1/charges").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(500));

        HttpClient client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestUser.Customer(customerId).HeaderValue);
        PlaceOrderRequest request = new([new OrderLineRequest(bookId, 1)], "Ankara");

        HttpResponseMessage first = await client.PostAsJsonAsync("/api/orders", request, Ct);
        int callsAfterFirst = factory.PaymentApi.LogEntries.Count();

        Stopwatch stopwatch = Stopwatch.StartNew();
        HttpResponseMessage second = await client.PostAsJsonAsync("/api/orders", request, Ct);
        stopwatch.Stop();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, first.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, second.StatusCode);
        Assert.Equal(4, callsAfterFirst);
        Assert.Equal(callsAfterFirst, factory.PaymentApi.LogEntries.Count());
    }
}
