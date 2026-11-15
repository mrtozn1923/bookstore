using System.Net;
using System.Net.Http.Json;
using Bookstore.Api.Endpoints;
using Bookstore.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.IntegrationTests;

public sealed class CatalogAuthorizationTests(BookstoreApiFactory factory) : IClassFixture<BookstoreApiFactory>, IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await factory.ResetDatabaseAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static CreateBookRequest NewBook(string isbn) => new(isbn, "Domain-Driven Design", 72.00m, 5);

    [Fact]
    public async Task Anonymous_user_gets_401()
    {
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/books", NewBook("978-0321125217"),
            TestContext.Current.CancellationToken);

        ProblemDetails problem = await response.AssertProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal("Unauthorized", problem.Title);
    }

    [Fact]
    public async Task Customer_without_admin_role_gets_403()
    {
        HttpClient client = factory.CreateClientFor(TestUser.Customer(42));

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/books", NewBook("978-0321125217"),
            TestContext.Current.CancellationToken);

        ProblemDetails problem = await response.AssertProblemAsync(HttpStatusCode.Forbidden);
        Assert.Equal("Forbidden", problem.Title);
    }

    [Fact]
    public async Task Admin_creates_book_and_gets_its_location()
    {
        HttpClient client = factory.CreateClientFor(TestUser.Admin);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/books", NewBook("978-0321125218"),
            TestContext.Current.CancellationToken);

        int id = response.AssertCreatedAt("/api/books/");
        BookDto? created = await client.GetFromJsonAsync<BookDto>($"/api/books/{id}", TestContext.Current.CancellationToken);
        Assert.Equal("978-0321125218", created!.Isbn);
    }

    [Fact]
    public async Task Invalid_book_returns_validation_problem()
    {
        HttpClient client = factory.CreateClientFor(TestUser.Admin);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/books",
            new CreateBookRequest("978-0321125219", "", 0m, 1), TestContext.Current.CancellationToken);

        HttpValidationProblemDetails problem = await response.AssertValidationProblemAsync();
        Assert.Equal(["price", "title"], problem.Errors.Keys.Order());
    }

    [Fact]
    public async Task Old_isbn_link_redirects_permanently_to_canonical_url()
    {
        HttpClient admin = factory.CreateClientFor(TestUser.Admin);
        HttpResponseMessage created = await admin.PostAsJsonAsync("/api/books", NewBook("978-0321125220"),
            TestContext.Current.CancellationToken);
        int id = created.AssertCreatedAt("/api/books/");

        HttpClient client = factory.CreateClientFor(TestUser.Customer(42), allowAutoRedirect: false);
        HttpResponseMessage response = await client.GetAsync("/books/isbn/978-0321125220", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal($"/api/books/{id}", response.Headers.Location?.OriginalString);
    }
}
