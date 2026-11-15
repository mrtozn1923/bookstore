using System.Net;
using System.Net.Http.Json;
using Bookstore.Catalog;
using Bookstore.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Tests;

public sealed class AuthorTestsFactory() : BookstoreApiFactory("_AuthorTests");

public sealed class AuthorTests(AuthorTestsFactory factory) : IClassFixture<AuthorTestsFactory>
{
    private const string Dune = "9780441172719";

    private readonly HttpClient client = factory.CreateClientFor("grace@example.com", "StoreStaff");
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Authors_are_listed_by_name_with_their_book_count()
    {
        List<AuthorView>? authors = await client.GetFromJsonAsync<List<AuthorView>>("/api/authors", ct);

        Assert.NotNull(authors);
        Assert.True(authors.Count >= 18);
        Assert.Equal(authors.OrderBy(a => a.Name, StringComparer.Ordinal).Select(a => a.Name), authors.Select(a => a.Name));
        Assert.Equal(2, authors.Single(a => a.Name == "Leo Tolstoy").BookCount);
    }

    [Fact]
    public async Task A_new_author_is_created_with_books_that_start_with_no_stock()
    {
        NewAuthorRequest request = new("Virginia Woolf",
        [
            new("9780156628709", "Mrs Dalloway", 259.00m, 1925, "classics"),
            new("9780156907392", "To the Lighthouse", 269.00m, 1927, "classics")
        ]);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/authors", request, ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        AuthorView? author = await response.Content.ReadFromJsonAsync<AuthorView>(ct);
        Assert.Equal(2, author!.BookCount);
        List<StockView>? stock = await client.GetFromJsonAsync<List<StockView>>("/api/inventory", ct);
        Assert.Equal(0, stock!.Single(s => s.Isbn == "9780156628709").OnHand);
    }

    [Fact]
    public async Task An_existing_isbn_is_reported_on_the_book_that_uses_it()
    {
        NewAuthorRequest request = new("Ada Lovelace",
        [
            new("9780000000019", "Notes", 199.00m, 1843, "classics"),
            new(Dune, "Not Dune", 199.00m, 1843, "no-such-category")
        ]);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/authors", request, ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        Assert.Equal(["Books[1].Isbn", "Books[1].CategorySlug"], problem!.Errors.Keys);
    }

    [Fact]
    public async Task Field_rules_of_nested_books_are_validated()
    {
        NewAuthorRequest request = new("", [new("12345", "", 0m, 1200, "classics")]);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/authors", request, ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        Assert.Contains("Name", problem!.Errors.Keys);
        Assert.Contains("Books[0].Isbn", problem.Errors.Keys);
        Assert.Contains("Books[0].Price", problem.Errors.Keys);
        Assert.Contains("Books[0].PublishedYear", problem.Errors.Keys);
    }
}
