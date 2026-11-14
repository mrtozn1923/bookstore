using System.Net;
using System.Net.Http.Json;
using Bookstore.Ordering;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Tests;

public sealed class CouponTests(BookstoreApiFactory factory)
{
    private const string Dune = "9780441172719";    // Science Fiction, 489.00
    private const string Emma = "9780141439587";    // Classics, 249.90

    private readonly HttpClient client = factory.CreateClient();
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task BOOK10_takes_ten_percent_of_the_whole_cart()
    {
        Guid cartId = await CreateCartWithAsync(Dune);

        CartView cart = await ApplyAsync(cartId, "BOOK10");

        Assert.Equal(489.00m, cart.Subtotal);
        Assert.Equal(48.90m, cart.Discount);
        Assert.Equal(440.10m, cart.Total);
    }

    [Fact]
    public async Task CLASSIC15_discounts_only_classics()
    {
        Guid cartId = await CreateCartWithAsync(Dune, Emma);

        CartView cart = await ApplyAsync(cartId, "classic15");

        // 15% of 249.90 = 37.485, rounded away from zero
        Assert.Equal("CLASSIC15", cart.CouponCode);
        Assert.Equal(37.49m, cart.Discount);
        Assert.Equal(701.41m, cart.Total);
    }

    [Fact]
    public async Task SAVE50_below_the_minimum_subtotal_is_rejected()
    {
        Guid cartId = await CreateCartWithAsync(Emma);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/carts/{cartId}/coupon", new ApplyCouponRequest("SAVE50"), ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ct);
        Assert.Equal("Coupon SAVE50 is not valid for this cart.", problem?.Detail);
    }

    [Fact]
    public async Task Unknown_coupon_is_rejected()
    {
        Guid cartId = await CreateCartWithAsync(Dune);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/carts/{cartId}/coupon", new ApplyCouponRequest("FREEBOOKS"), ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Order_keeps_the_coupon_discount()
    {
        Guid cartId = await CreateCartWithAsync(Dune);
        await ApplyAsync(cartId, "BOOK10");

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/orders",
            new PlaceOrderRequest(cartId, "Margaret Hamilton", "margaret@example.com"), ct);

        OrderView? order = await response.Content.ReadFromJsonAsync<OrderView>(ct);
        Assert.Equal("BOOK10", order?.CouponCode);
        Assert.Equal(489.00m, order?.Subtotal);
        Assert.Equal(48.90m, order?.Discount);
        Assert.Equal(440.10m, order?.Total);
    }

    private async Task<CartView> ApplyAsync(Guid cartId, string code)
    {
        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/carts/{cartId}/coupon", new ApplyCouponRequest(code), ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CartView>(ct))!;
    }

    private async Task<Guid> CreateCartWithAsync(params string[] isbns)
    {
        CartView? cart = await (await client.PostAsync("/api/carts", null, ct)).Content.ReadFromJsonAsync<CartView>(ct);
        foreach (string isbn in isbns)
        {
            await client.PutAsJsonAsync($"/api/carts/{cart!.Id}/items/{isbn}", new SetQuantityRequest(1), ct);
        }
        return cart!.Id;
    }
}
