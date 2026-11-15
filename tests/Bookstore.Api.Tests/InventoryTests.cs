using System.Net;
using System.Net.Http.Json;
using Bookstore.Inventory;
using Bookstore.Ordering;

namespace Bookstore.Api.Tests;

public sealed class InventoryTests(BookstoreApiFactory factory)
{
    private const string Emma = "9780141439587";
    private const string Middlemarch = "9780141439549";
    private const string DonQuixote = "9780060934347";   // seeded with 0 copies

    private readonly HttpClient client = factory.CreateClientFor("linus@example.com", "WarehouseStaff");
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Stock_list_starts_with_the_scarcest_book()
    {
        List<StockView>? stock = await client.GetFromJsonAsync<List<StockView>>("/api/inventory", ct);

        Assert.NotNull(stock);
        Assert.Equal(20, stock.Count);
        Assert.Equal(DonQuixote, stock[0].Isbn);
        Assert.Equal(0, stock[0].Available);
    }

    [Fact]
    public async Task Setting_quantity_on_hand_changes_the_available_stock()
    {
        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/inventory/{Emma}", new SetStockRequest(40), ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        List<StockView>? stock = await client.GetFromJsonAsync<List<StockView>>("/api/inventory", ct);
        Assert.Equal(40, stock!.Single(s => s.Isbn == Emma).OnHand);
    }

    [Fact]
    public async Task Quantity_on_hand_cannot_drop_below_the_reserved_quantity()
    {
        HttpResponseMessage created = await client.PostAsync("/api/carts", null, ct);
        CartView? cart = await created.Content.ReadFromJsonAsync<CartView>(ct);
        await client.PutAsJsonAsync($"/api/carts/{cart!.Id}/items/{Middlemarch}", new SetQuantityRequest(2), ct);
        HttpResponseMessage ordered = await client.PostAsJsonAsync(
            "/api/orders", new PlaceOrderRequest(cart.Id, "Grace Hopper", "grace@example.com"), ct);
        ordered.EnsureSuccessStatusCode();

        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/inventory/{Middlemarch}", new SetStockRequest(1), ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10_001)]
    public async Task Quantity_outside_0_to_10000_is_a_validation_error(int quantity)
    {
        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/inventory/{Emma}", new SetStockRequest(quantity), ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_isbn_returns_404()
    {
        HttpResponseMessage response = await client.PutAsJsonAsync("/api/inventory/9780000000000", new SetStockRequest(5), ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
