using MongoDB.Bson;
using MongoDB.Driver;

namespace Bookstore.MongoLab;

public static class StockLab
{
    private const int BookId = 26;
    private const int InitialStock = 10;
    private const int Buyers = 50;

    public static async Task RaceAsync(string connectionString)
    {
        IMongoCollection<BsonDocument> books = new MongoClient(connectionString)
            .GetDatabase("bookstore")
            .GetCollection<BsonDocument>("books");

        await RunAsync(books, "read-then-write", ReadThenWriteAsync);
        await RunAsync(books, "conditional $inc", ConditionalIncAsync);

        await books.UpdateOneAsync(new BsonDocument("_id", BookId),
            new BsonDocument("$set", new BsonDocument("stock.onHand", 51)));
    }

    private static async Task RunAsync(IMongoCollection<BsonDocument> books, string name,
        Func<IMongoCollection<BsonDocument>, Task<bool>> buy)
    {
        await books.UpdateOneAsync(new BsonDocument("_id", BookId),
            new BsonDocument("$set", new BsonDocument("stock.onHand", InitialStock)));

        bool[] results = await Task.WhenAll(Enumerable.Range(0, Buyers).Select(_ => Task.Run(() => buy(books))));

        BsonDocument book = await books.Find(new BsonDocument("_id", BookId)).FirstAsync();
        int sold = results.Count(r => r);
        Console.WriteLine($"{name,-17}: sold {sold,2} of {InitialStock}, stock.onHand = {book["stock"]["onHand"]}");
    }

    // Wrong: read, check in the application, then write the new value
    private static async Task<bool> ReadThenWriteAsync(IMongoCollection<BsonDocument> books)
    {
        BsonDocument book = await books.Find(new BsonDocument("_id", BookId)).FirstAsync();
        int onHand = book["stock"]["onHand"].AsInt32;
        if (onHand < 1)
        {
            return false;
        }

        await Task.Delay(5); // application work such as price and campaign checks
        await books.UpdateOneAsync(new BsonDocument("_id", BookId),
            new BsonDocument("$set", new BsonDocument("stock.onHand", onHand - 1)));
        return true;
    }

    // Right: the check and the decrement in one atomic update
    private static async Task<bool> ConditionalIncAsync(IMongoCollection<BsonDocument> books)
    {
        UpdateResult result = await books.UpdateOneAsync(
            new BsonDocument { { "_id", BookId }, { "stock.onHand", new BsonDocument("$gte", 1) } },
            new BsonDocument("$inc", new BsonDocument("stock.onHand", -1)));
        return result.ModifiedCount == 1;
    }
}
