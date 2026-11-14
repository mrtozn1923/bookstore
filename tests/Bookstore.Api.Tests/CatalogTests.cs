using System.Net;
using System.Net.Http.Json;
using Bookstore.Catalog;

namespace Bookstore.Api.Tests;

public sealed class CatalogTests(BookstoreApiFactory factory)
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Search_finds_books_by_author_name()
    {
        PagedResult<BookSummary>? result = await client.GetFromJsonAsync<PagedResult<BookSummary>>(
            "/api/books?search=austen", TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(["Emma", "Pride and Prejudice"], result.Items.Select(b => b.Title));
    }

    [Fact]
    public async Task Seed_contains_twenty_books_in_four_categories()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        PagedResult<BookSummary>? books = await client.GetFromJsonAsync<PagedResult<BookSummary>>("/api/books?pageSize=50", ct);
        List<CategoryView>? categories = await client.GetFromJsonAsync<List<CategoryView>>("/api/categories", ct);

        Assert.Equal(20, books?.Total);
        Assert.Equal(4, categories?.Count);
    }

    [Fact]
    public async Task Similar_books_share_the_author_or_the_category()
    {
        // Emma (Jane Austen, Classics): Pride and Prejudice comes first because of the author.
        List<BookSummary>? similar = await client.GetFromJsonAsync<List<BookSummary>>(
            "/api/books/9780141439587/similar", TestContext.Current.CancellationToken);

        Assert.NotNull(similar);
        Assert.InRange(similar.Count, 1, 4);
        Assert.Equal("Pride and Prejudice", similar[0].Title);
        Assert.All(similar, b => Assert.True(b.Author == "Jane Austen" || b.Category == "Classics"));
    }

    [Fact]
    public async Task Similar_books_for_an_unknown_isbn_returns_404()
    {
        HttpResponseMessage response = await client.GetAsync(
            "/api/books/9780000000000/similar", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_isbn_returns_404()
    {
        HttpResponseMessage response = await client.GetAsync("/api/books/9780000000000", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
