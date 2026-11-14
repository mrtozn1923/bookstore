using MongoDB.Driver;

namespace Bookstore.MongoLab;

public sealed class InsufficientStockException(int bookId) : Exception($"Not enough stock for book {bookId}")
{
    public int BookId { get; } = bookId;
}

// Places an order: all stock decrements and the order insert succeed together or not at all
public sealed class OrderPlacement(IMongoClient client, IMongoCollection<Book> books, IMongoCollection<Order> orders)
{
    private int _attempts;

    // How many times the transaction body ran (retries included)
    public int Attempts => _attempts;

    public async Task<Order> PlaceAsync(Order order, CancellationToken cancellationToken = default)
    {
        using IClientSessionHandle session = await client.StartSessionAsync(cancellationToken: cancellationToken);
        return await session.WithTransactionAsync(async (s, ct) =>
        {
            Interlocked.Increment(ref _attempts);
            foreach (OrderItem item in order.Items)
            {
                UpdateResult result = await books.UpdateOneAsync(s,
                    b => b.Id == item.BookId && b.Stock!.OnHand >= item.Quantity,
                    Builders<Book>.Update.Inc(b => b.Stock!.OnHand, -item.Quantity),
                    cancellationToken: ct);
                if (result.ModifiedCount == 0)
                {
                    throw new InsufficientStockException(item.BookId);   // aborts the transaction
                }
            }
            await orders.InsertOneAsync(s, order, cancellationToken: ct);
            return order;
        }, cancellationToken: cancellationToken);
    }
}
