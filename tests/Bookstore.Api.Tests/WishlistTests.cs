using System.Net;
using System.Net.Http.Json;
using Bookstore.Wishlists;

namespace Bookstore.Api.Tests;

public sealed class WishlistTests(BookstoreApiFactory factory)
{
    private const string TheHobbit = "9780261102217";
    private const string WarAndPeace = "9781400079988";

    private readonly HttpClient client = factory.CreateClient();
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Creating_a_wishlist_returns_201_and_its_location()
    {
        HttpResponseMessage response = await client.PostAsync("/api/wishlists", null, ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        WishlistView? wishlist = await response.Content.ReadFromJsonAsync<WishlistView>(ct);
        Assert.EndsWith($"/api/wishlists/{wishlist!.Id}", response.Headers.Location?.ToString());
        Assert.Empty(wishlist.Items);
    }

    [Fact]
    public async Task Added_books_show_current_title_and_price_ordered_by_title()
    {
        Guid id = await CreateWishlistAsync();
        await client.PutAsync($"/api/wishlists/{id}/items/{WarAndPeace}", null, ct);
        HttpResponseMessage response = await client.PutAsync($"/api/wishlists/{id}/items/{TheHobbit}", null, ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        WishlistView? wishlist = await response.Content.ReadFromJsonAsync<WishlistView>(ct);
        Assert.Equal(["The Hobbit", "War and Peace"], wishlist!.Items.Select(i => i.Title));
        Assert.Equal(329.90m, wishlist.Items[0].Price);
    }

    [Fact]
    public async Task Adding_the_same_book_twice_keeps_one_line()
    {
        Guid id = await CreateWishlistAsync();
        await client.PutAsync($"/api/wishlists/{id}/items/{TheHobbit}", null, ct);
        HttpResponseMessage second = await client.PutAsync($"/api/wishlists/{id}/items/{TheHobbit}", null, ct);

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        WishlistView? wishlist = await second.Content.ReadFromJsonAsync<WishlistView>(ct);
        Assert.Single(wishlist!.Items);
    }

    [Fact]
    public async Task Removing_a_book_returns_the_updated_wishlist()
    {
        Guid id = await CreateWishlistAsync();
        await client.PutAsync($"/api/wishlists/{id}/items/{TheHobbit}", null, ct);

        HttpResponseMessage response = await client.DeleteAsync($"/api/wishlists/{id}/items/{TheHobbit}", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        WishlistView? wishlist = await response.Content.ReadFromJsonAsync<WishlistView>(ct);
        Assert.Empty(wishlist!.Items);
    }

    [Fact]
    public async Task Unknown_wishlist_or_book_returns_404()
    {
        Guid id = await CreateWishlistAsync();

        HttpResponseMessage unknownWishlist = await client.PutAsync($"/api/wishlists/{Guid.NewGuid()}/items/{TheHobbit}", null, ct);
        HttpResponseMessage unknownBook = await client.PutAsync($"/api/wishlists/{id}/items/9780000000000", null, ct);
        HttpResponseMessage get = await client.GetAsync($"/api/wishlists/{Guid.NewGuid()}", ct);

        Assert.Equal(HttpStatusCode.NotFound, unknownWishlist.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknownBook.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    private async Task<Guid> CreateWishlistAsync()
    {
        HttpResponseMessage response = await client.PostAsync("/api/wishlists", null, ct);
        WishlistView? wishlist = await response.Content.ReadFromJsonAsync<WishlistView>(ct);
        return wishlist!.Id;
    }
}
