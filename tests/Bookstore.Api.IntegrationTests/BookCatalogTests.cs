using System.Net;
using System.Net.Http.Json;
using Bookstore.Api.Data;
using Bookstore.Api.Endpoints;
using Microsoft.Extensions.DependencyInjection;

namespace Bookstore.Api.IntegrationTests;

public sealed class BookCatalogTests(BookstoreApiFactory factory) : IClassFixture<BookstoreApiFactory>, IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await factory.ResetDatabaseAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Search_is_case_insensitive_like_production()
    {
        using (IServiceScope scope = factory.Services.CreateScope())
        {
            BookstoreDbContext db = scope.ServiceProvider.GetRequiredService<BookstoreDbContext>();
            db.Books.Add(new Book { Isbn = "978-0132350884", Title = "Clean Code", Price = 54.90m, Stock = 12 });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        HttpClient client = factory.CreateClient();
        List<BookDto>? books = await client.GetFromJsonAsync<List<BookDto>>(
            "/api/books?search=clean code", TestContext.Current.CancellationToken);

        BookDto book = Assert.Single(books!);
        Assert.Equal("Clean Code", book.Title);
        Assert.Equal(54.90m, book.Price);
    }

    [Fact]
    public async Task Unknown_book_returns_404_problem_details()
    {
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/books/999999", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
