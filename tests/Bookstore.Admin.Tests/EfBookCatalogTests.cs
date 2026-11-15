using Bookstore.Admin.Client.Contracts;
using Bookstore.Admin.Services;
using Bookstore.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Admin.Tests;

// EfBookCatalog against an in-memory SQLite database created from BookstoreContext's model and seed data.
public sealed class EfBookCatalogTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private EfBookCatalog catalog = null!;

    public async ValueTask InitializeAsync()
    {
        await connection.OpenAsync();
        DbContextOptions<BookstoreContext> options = new DbContextOptionsBuilder<BookstoreContext>().UseSqlite(connection).Options;
        await using (BookstoreContext db = new(options))
        {
            await db.Database.EnsureCreatedAsync();
        }
        catalog = new EfBookCatalog(new TestDbContextFactory(options));
    }

    public async ValueTask DisposeAsync() => await connection.DisposeAsync();

    [Fact]
    public async Task Searches_by_author_and_pages_by_title()
    {
        PagedResult<BookSummary> austen = await catalog.SearchAsync("Austen", page: 1, pageSize: 10, Xunit.TestContext.Current.CancellationToken);
        PagedResult<BookSummary> second = await catalog.SearchAsync(null, page: 2, pageSize: 5, Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(["Emma", "Pride and Prejudice"], austen.Items.Select(b => b.Title));
        Assert.Equal(20, second.Total);
        Assert.Equal("Dune", second.Items[0].Title);
    }

    private sealed class TestDbContextFactory(DbContextOptions<BookstoreContext> options) : IDbContextFactory<BookstoreContext>
    {
        public BookstoreContext CreateDbContext() => new(options);
    }
}
