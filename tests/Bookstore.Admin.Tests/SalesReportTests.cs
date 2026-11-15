using Bookstore.Admin.Services;
using Bookstore.Data;
using Bookstore.Ordering;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Admin.Tests;

public sealed class SalesReportTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private DbContextOptions<BookstoreContext> options = null!;

    public async ValueTask InitializeAsync()
    {
        await connection.OpenAsync();
        options = new DbContextOptionsBuilder<BookstoreContext>().UseSqlite(connection).Options;
        await using BookstoreContext db = new(options);
        await db.Database.EnsureCreatedAsync();
        Customer ada = new() { Name = "Ada Lovelace", Email = "ada@example.com" };
        db.Orders.AddRange(
            Order(ada, OrderStatus.Paid, (1, "Dune", 3), (17, "Foundation", 2), (3, "Emma", 4)),
            Order(ada, OrderStatus.Cancelled, (1, "Dune", 10)));
        await db.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync() => await connection.DisposeAsync();

    [Fact]
    public async Task Copies_are_summed_per_category_without_cancelled_orders()
    {
        SalesReport report = new(new Factory(options));

        List<CategorySales> sales = await report.GetCopiesByCategoryAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Equal([new("Classics", "classics", 4), new("Science Fiction", "science-fiction", 5)], sales);
    }

    private static Order Order(Customer customer, OrderStatus status, params (int BookId, string Title, int Quantity)[] lines)
    {
        Order order = new() { Customer = customer, Status = status, PlacedAtUtc = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc) };
        order.Items.AddRange(lines.Select(l => new OrderItem { BookId = l.BookId, Title = l.Title, Quantity = l.Quantity }));
        return order;
    }

    private sealed class Factory(DbContextOptions<BookstoreContext> options) : IDbContextFactory<BookstoreContext>
    {
        public BookstoreContext CreateDbContext() => new(options);
    }
}
