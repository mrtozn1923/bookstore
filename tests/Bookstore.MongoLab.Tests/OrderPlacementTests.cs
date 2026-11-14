using MongoDB.Driver;

namespace Bookstore.MongoLab.Tests;

public sealed class OrderPlacementTests(MongoFixture mongo) : IClassFixture<MongoFixture>
{
    [Fact]
    public async Task Order_decrements_stock_of_every_book_and_is_saved()
    {
        (OrderPlacement placement, IMongoCollection<Book> books, IMongoCollection<Order> orders) = await SeedAsync();

        await placement.PlaceAsync(NewOrder(1, (26, 2), (35, 1)), TestContext.Current.CancellationToken);

        Assert.Equal(1, (await books.Find(b => b.Id == 26).FirstAsync(TestContext.Current.CancellationToken)).Stock!.OnHand);
        Assert.Equal(0, (await books.Find(b => b.Id == 35).FirstAsync(TestContext.Current.CancellationToken)).Stock!.OnHand);
        Assert.Equal(1, await orders.CountDocumentsAsync(o => o.Id == 1, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Insufficient_stock_rolls_back_the_whole_order()
    {
        (OrderPlacement placement, IMongoCollection<Book> books, IMongoCollection<Order> orders) = await SeedAsync();

        await Assert.ThrowsAsync<InsufficientStockException>(() =>
            placement.PlaceAsync(NewOrder(2, (26, 1), (35, 5)), TestContext.Current.CancellationToken));

        // Dune was decremented first, then the transaction was aborted
        Assert.Equal(3, (await books.Find(b => b.Id == 26).FirstAsync(TestContext.Current.CancellationToken)).Stock!.OnHand);
        Assert.Equal(0, await orders.CountDocumentsAsync(o => o.Id == 2, cancellationToken: TestContext.Current.CancellationToken));
    }

    // Each test gets its own database, so tests do not see each other's data
    private async Task<(OrderPlacement, IMongoCollection<Book>, IMongoCollection<Order>)> SeedAsync()
    {
        IMongoDatabase database = mongo.Client.GetDatabase($"bookstore_{Guid.NewGuid():N}");
        IMongoCollection<Book> books = database.GetCollection<Book>("books");
        IMongoCollection<Order> orders = database.GetCollection<Order>("orders");
        await database.CreateCollectionAsync("orders");   // collections are created outside the transaction
        await books.InsertManyAsync(
        [
            NewBook(26, "Dune", 3),
            NewBook(35, "Nineteen Eighty-Four", 1)
        ]);
        return (new OrderPlacement(mongo.Client, books, orders), books, orders);
    }

    private static Book NewBook(int id, string title, int onHand) => new()
    {
        Id = id, Isbn = $"97800000000{id:00}", Title = title, Language = "en", ListPrice = 100m,
        Stock = new Stock(onHand, 5, new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc))
    };

    private static Order NewOrder(int id, params (int BookId, int Quantity)[] items) => new()
    {
        Id = id,
        Customer = new CustomerRef(3, "Grace Hopper"),
        OrderedAt = new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc),
        Items = [.. items.Select(i => new OrderItem(i.BookId, $"Book {i.BookId}", i.Quantity, 100m))],
        Total = items.Sum(i => i.Quantity * 100m),
        Currency = "TRY"
    };
}
