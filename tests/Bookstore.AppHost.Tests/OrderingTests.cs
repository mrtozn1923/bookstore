namespace Bookstore.AppHost.Tests;

public sealed class OrderingFixture() : BookstoreAppFixture("redis", "keycloak", "web", "web-installer")
{
    protected override async Task WaitUntilReadyAsync(DistributedApplication app, CancellationToken ct)
    {
        await app.ResourceNotifications.WaitForResourceHealthyAsync("ordering", ct);
        await app.ResourceNotifications.WaitForResourceAsync("inventory", KnownResourceStates.Running, ct);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("gateway", ct);
    }
}

public sealed class OrderingTests(OrderingFixture fixture) : IClassFixture<OrderingFixture>
{
    private sealed record OrderDto(Guid Id, decimal Total, string Status);

    [Fact]
    public async Task PlacedOrderIsConfirmedAfterStockIsReserved()
    {
        using HttpClient gateway = fixture.App.CreateHttpClient("gateway");

        using HttpResponseMessage response = await gateway.PostAsJsonAsync("/api/orders/", new
        {
            customerEmail = "ada@example.com",
            items = new[] { new { isbn = "9780441172719", quantity = 1 }, new { isbn = "9780547928227", quantity = 2 } }
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        OrderDto? placed = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal("Pending", placed!.Status);
        Assert.Equal(1100.00m, placed.Total);

        // Confirmation arrives over RabbitMQ: ask again instead of sleeping a fixed time
        OrderDto confirmed = await Polling.UntilAsync(
            async ct => (await gateway.GetFromJsonAsync<OrderDto>($"/api/orders/{placed.Id}", ct))!,
            order => order.Status == "Confirmed",
            TimeSpan.FromSeconds(10));

        Assert.Equal("Confirmed", confirmed.Status);
    }
}
