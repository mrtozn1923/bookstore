using System.Net;
using System.Net.Http.Json;
using Bookstore.Ordering;
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

    [Fact]
    public async Task Moving_a_book_puts_one_copy_in_the_cart_and_removes_it_from_the_wishlist()
    {
        Guid wishlistId = await CreateWishlistWithAsync(TheHobbit);
        Guid cartId = await CreateCartAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/wishlists/{wishlistId}/items/{TheHobbit}/move-to-cart", new MoveToCartRequest(cartId), ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        WishlistView? wishlist = await response.Content.ReadFromJsonAsync<WishlistView>(ct);
        Assert.Empty(wishlist!.Items);
        CartView? cart = await client.GetFromJsonAsync<CartView>($"/api/carts/{cartId}", ct);
        Assert.Equal(1, Assert.Single(cart!.Items).Quantity);
    }

    [Fact]
    public async Task Moving_a_book_that_is_already_in_the_cart_increases_its_quantity()
    {
        Guid wishlistId = await CreateWishlistWithAsync(TheHobbit);
        Guid cartId = await CreateCartAsync();
        await client.PutAsJsonAsync($"/api/carts/{cartId}/items/{TheHobbit}", new SetQuantityRequest(1), ct);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/wishlists/{wishlistId}/items/{TheHobbit}/move-to-cart", new MoveToCartRequest(cartId), ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        CartView? cart = await client.GetFromJsonAsync<CartView>($"/api/carts/{cartId}", ct);
        Assert.Equal(2, Assert.Single(cart!.Items).Quantity);
    }

    [Fact]
    public async Task Moving_to_an_unknown_cart_keeps_the_book_on_the_wishlist()
    {
        Guid wishlistId = await CreateWishlistWithAsync(TheHobbit);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/wishlists/{wishlistId}/items/{TheHobbit}/move-to-cart", new MoveToCartRequest(Guid.NewGuid()), ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        WishlistView? wishlist = await client.GetFromJsonAsync<WishlistView>($"/api/wishlists/{wishlistId}", ct);
        Assert.Single(wishlist!.Items);
    }

    private async Task<Guid> CreateWishlistWithAsync(string isbn)
    {
        Guid id = await CreateWishlistAsync();
        (await client.PutAsync($"/api/wishlists/{id}/items/{isbn}", null, ct)).EnsureSuccessStatusCode();
        return id;
    }

    private async Task<Guid> CreateCartAsync()
    {
        HttpResponseMessage response = await client.PostAsync("/api/carts", null, ct);
        CartView? cart = await response.Content.ReadFromJsonAsync<CartView>(ct);
        return cart!.Id;
    }

    private async Task<Guid> CreateWishlistAsync()
    {
        HttpResponseMessage response = await client.PostAsync("/api/wishlists", null, ct);
        WishlistView? wishlist = await response.Content.ReadFromJsonAsync<WishlistView>(ct);
        return wishlist!.Id;
    }
}
