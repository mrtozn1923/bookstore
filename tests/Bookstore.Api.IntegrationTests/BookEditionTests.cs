using System.Net.Http.Json;
using Bookstore.Api.Data;
using Bookstore.Api.Endpoints;
using Microsoft.Extensions.DependencyInjection;

namespace Bookstore.Api.IntegrationTests;

public sealed class BookEditionTests(BookstoreApiFactory factory) : IClassFixture<BookstoreApiFactory>, IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await factory.ResetDatabaseAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Second_edition_is_listed_with_its_own_price()
    {
        using (IServiceScope scope = factory.Services.CreateScope())
        {
            BookstoreDbContext db = scope.ServiceProvider.GetRequiredService<BookstoreDbContext>();
            db.Books.Add(new Book { Isbn = "978-0135398579", Title = "Clean Code, 2nd Edition", Price = 61.50m, Stock = 4 });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        HttpClient client = factory.CreateClient();
        List<BookDto>? books = await client.GetFromJsonAsync<List<BookDto>>(
            "/api/books?search=2nd edition", TestContext.Current.CancellationToken);

        Assert.Equal(61.50m, Assert.Single(books!).Price);
    }
}
