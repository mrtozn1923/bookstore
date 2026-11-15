using System.Net;
using System.Net.Http.Json;
using Bookstore.Catalog;
using Bookstore.Ordering;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Tests;

public sealed class OrderingTests(BookstoreApiFactory factory)
{
    private const string TheHobbit = "9780261102217";
    private const string WarAndPeace = "9781400079988";   // seeded with 3 copies

    private readonly HttpClient client = factory.CreateClient();
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Placing_an_order_reserves_stock_and_removes_the_cart()
    {
        Guid cartId = await CreateCartWithAsync(TheHobbit, quantity: 2);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/orders",
            new PlaceOrderRequest(cartId, "Ada Lovelace", "ada@example.com"), ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        OrderView? order = await response.Content.ReadFromJsonAsync<OrderView>(ct);
        Assert.Equal("Placed", order?.Status);
        Assert.Equal(659.80m, order?.Total);

        BookDetails? book = await client.GetFromJsonAsync<BookDetails>($"/api/books/{TheHobbit}", ct);
        Assert.Equal(23, book?.Available);

        HttpResponseMessage cart = await client.GetAsync($"/api/carts/{cartId}", ct);
        Assert.Equal(HttpStatusCode.NotFound, cart.StatusCode);
    }

    [Fact]
    public async Task Ordering_more_than_available_returns_409()
    {
        Guid cartId = await CreateCartWithAsync(WarAndPeace, quantity: 5);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/orders",
            new PlaceOrderRequest(cartId, "Alan Turing", "alan@example.com"), ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ct);
        Assert.Contains(WarAndPeace, problem?.Detail);
    }

    [Fact]
    [Trait("asvs", "v5.0.0-2.3.4")]
    public async Task Two_parallel_orders_cannot_reserve_more_than_the_stock()
    {
        // Four copies are wanted in total, three are in stock; exactly one of the two orders can succeed.
        Guid first = await CreateCartWithAsync(WarAndPeace, quantity: 2);
        Guid second = await CreateCartWithAsync(WarAndPeace, quantity: 2);

        HttpResponseMessage[] responses = await Task.WhenAll(
            client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest(first, "Grace Hopper", "grace@example.com"), ct),
            client.PostAsJsonAsync("/api/orders", new PlaceOrderRequest(second, "Linus Torvalds", "linus@example.com"), ct));

        Assert.Equal(
            [HttpStatusCode.Created, HttpStatusCode.Conflict],
            responses.Select(r => r.StatusCode).Order());
        BookDetails? book = await client.GetFromJsonAsync<BookDetails>($"/api/books/{WarAndPeace}", ct);
        Assert.Equal(1, book?.Available);
    }

    [Fact]
    public async Task Quantity_outside_1_to_99_is_a_validation_error()
    {
        CartView? cart = await (await client.PostAsync("/api/carts", null, ct)).Content.ReadFromJsonAsync<CartView>(ct);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/carts/{cart!.Id}/items/{TheHobbit}", new SetQuantityRequest(0), ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        HttpValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(ct);
        Assert.True(problem?.Errors.ContainsKey("Quantity"));
    }

    private async Task<Guid> CreateCartWithAsync(string isbn, int quantity)
    {
        HttpResponseMessage created = await client.PostAsync("/api/carts", null, ct);
        CartView? cart = await created.Content.ReadFromJsonAsync<CartView>(ct);
        HttpResponseMessage added = await client.PutAsJsonAsync(
            $"/api/carts/{cart!.Id}/items/{isbn}", new SetQuantityRequest(quantity), ct);
        added.EnsureSuccessStatusCode();
        return cart.Id;
    }
}
