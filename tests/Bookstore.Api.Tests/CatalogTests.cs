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
    public async Task Unknown_isbn_returns_404()
    {
        HttpResponseMessage response = await client.GetAsync("/api/books/9780000000000", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
