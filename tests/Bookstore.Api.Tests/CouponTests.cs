using System.Net;
using System.Net.Http.Json;
using Bookstore.Ordering;

namespace Bookstore.Api.Tests;

public sealed class CouponTests(BookstoreApiFactory factory)
{
    private const string Dune = "9780441172719";

    private readonly HttpClient client = factory.CreateClient();
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Applying_BOOK10_sets_the_discount()
    {
        Guid cartId = await CreateCartWithAsync(Dune, quantity: 1);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/carts/{cartId}/coupon", new ApplyCouponRequest("BOOK10"), ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        CartView? cart = await response.Content.ReadFromJsonAsync<CartView>(ct);
        Assert.Equal(Math.Round(cart!.Subtotal * 10 / 100m, 2), cart.Discount);
        Assert.Equal(cart.Subtotal - cart.Discount, cart.Total);
    }

    [Fact]
    public async Task Order_keeps_the_coupon_discount()
    {
        Guid cartId = await CreateCartWithAsync(Dune, quantity: 1);
        await client.PutAsJsonAsync($"/api/carts/{cartId}/coupon", new ApplyCouponRequest("BOOK10"), ct);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/orders",
            new PlaceOrderRequest(cartId, "Margaret Hamilton", "margaret@example.com"), ct);

        OrderView? order = await response.Content.ReadFromJsonAsync<OrderView>(ct);
        Assert.Equal("BOOK10", order?.CouponCode);
        Assert.Equal(order!.Subtotal - order.Discount, order.Total);
    }

    private async Task<Guid> CreateCartWithAsync(string isbn, int quantity)
    {
        CartView? cart = await (await client.PostAsync("/api/carts", null, ct)).Content.ReadFromJsonAsync<CartView>(ct);
        await client.PutAsJsonAsync($"/api/carts/{cart!.Id}/items/{isbn}", new SetQuantityRequest(quantity), ct);
        return cart.Id;
    }
}
