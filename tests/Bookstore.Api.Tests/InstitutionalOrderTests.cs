using System.Net;
using System.Net.Http.Json;
using Bookstore.Ordering;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Tests;

public sealed class InstitutionalOrderTests(BookstoreApiFactory factory)
{
    private const string Foundation = "9780553293357";
    private const string Middlemarch = "9780141439549";
    private const string Emma = "9780141439587";

    private readonly HttpClient client = factory.CreateClient();
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Institutional_order_with_twenty_books_across_lines_is_placed()
    {
        Guid cartId = await CreateCartAsync((Foundation, 12), (Middlemarch, 8));

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/orders",
            new PlaceOrderRequest(cartId, "Barbara Liskov", "barbara@example.com", InstitutionCode: "RHS"), ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Institutional_order_below_twenty_books_is_rejected()
    {
        Guid cartId = await CreateCartAsync((Emma, 5));

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/orders",
            new PlaceOrderRequest(cartId, "Edsger Dijkstra", "edsger@example.com", InstitutionCode: "RHS"), ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ct);
        Assert.Equal("Institutional orders need at least 20 books; this cart has 5.", problem?.Detail);
    }

    [Fact]
    public async Task Unknown_institution_code_is_rejected()
    {
        Guid cartId = await CreateCartAsync((Emma, 1));

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/orders",
            new PlaceOrderRequest(cartId, "Donald Knuth", "donald@example.com", InstitutionCode: "NOPE"), ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ct);
        Assert.Equal("Unknown institution code 'NOPE'.", problem?.Detail);
    }

    private async Task<Guid> CreateCartAsync(params (string Isbn, int Quantity)[] lines)
    {
        CartView? cart = await (await client.PostAsync("/api/carts", null, ct)).Content.ReadFromJsonAsync<CartView>(ct);
        foreach ((string isbn, int quantity) in lines)
        {
            HttpResponseMessage added = await client.PutAsJsonAsync(
                $"/api/carts/{cart!.Id}/items/{isbn}", new SetQuantityRequest(quantity), ct);
            added.EnsureSuccessStatusCode();
        }
        return cart!.Id;
    }
}
