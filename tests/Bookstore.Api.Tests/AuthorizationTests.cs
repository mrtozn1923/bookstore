using System.Net;
using System.Net.Http.Json;
using Bookstore.Catalog;
using Bookstore.Inventory;

namespace Bookstore.Api.Tests;

public sealed class AuthorizationTestsFactory() : BookstoreApiFactory("_AuthorizationTests");

public sealed class AuthorizationTests(AuthorizationTestsFactory factory) : IClassFixture<AuthorizationTestsFactory>
{
    private const string Emma = "9780141439587";

    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task The_storefront_catalog_stays_public()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/api/books?pageSize=1", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/inventory")]
    [InlineData("GET", "/api/authors")]
    [InlineData("POST", "/api/authors")]
    [InlineData("PUT", "/api/books/9780141439587/price")]
    public async Task Back_office_endpoints_reject_anonymous_calls(string method, string uri)
    {
        HttpRequestMessage request = new(new HttpMethod(method), uri) { Content = JsonContent.Create(new { }) };

        HttpResponseMessage response = await factory.CreateClient().SendAsync(request, ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Warehouse_staff_count_stock_but_cannot_change_prices()
    {
        HttpClient linus = factory.CreateClientFor("linus@example.com", "WarehouseStaff");

        HttpResponseMessage stock = await linus.PutAsJsonAsync($"/api/inventory/{Emma}", new SetStockRequest(25), ct);
        HttpResponseMessage price = await linus.PutAsJsonAsync($"/api/books/{Emma}/price", new SetPriceRequest(1m), ct);

        Assert.Equal(HttpStatusCode.NoContent, stock.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, price.StatusCode);
    }

    [Fact]
    public async Task Store_staff_change_prices()
    {
        HttpClient grace = factory.CreateClientFor("grace@example.com", "StoreStaff");

        HttpResponseMessage response = await grace.PutAsJsonAsync($"/api/books/{Emma}/price", new SetPriceRequest(289.50m), ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        BookDetails? book = await grace.GetFromJsonAsync<BookDetails>($"/api/books/{Emma}", ct);
        Assert.Equal(289.50m, book!.Price);
    }

    [Fact]
    public async Task A_token_signed_with_another_key_is_rejected()
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", TestTokens.ForKey(
            "some-other-key-that-the-api-does-not-know", "grace@example.com", "StoreStaff"));

        HttpResponseMessage response = await client.GetAsync("/api/inventory", ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
