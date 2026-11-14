using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace Bookstore.MongoLab.EfCore;

public static class EfLab
{
    private static readonly HashSet<string> Watched = ["find", "aggregate", "insert", "update", "delete", "commitTransaction"];
    private static bool _showCommands;

    // Same DI setup as the driver (post 9) plus the DbContext; command monitoring shows what EF Core sends
    public static ServiceProvider BuildServices(string connectionString) =>
        new ServiceCollection()
            .AddBookstoreMongo(connectionString, settings => settings.ClusterConfigurator = cluster =>
                cluster.Subscribe<CommandStartedEvent>(e =>
                {
                    if (_showCommands && Watched.Contains(e.CommandName))
                    {
                        Console.WriteLine("  > " + Describe(e.Command));
                    }
                }))
            .AddBookstoreEfCore()
            .BuildServiceProvider();

    private static string Describe(BsonDocument command) =>
        new BsonDocument(command.Where(e => e.Name is not ("lsid" or "$clusterTime" or "$db" or "$readPreference" or "txnNumber" or "cursor"))).ToJson();

    public static async Task SaveAsync(IServiceProvider services)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        BookstoreDbContext db = scope.ServiceProvider.GetRequiredService<BookstoreDbContext>();

        Book dune = await db.Books.SingleAsync(b => b.Id == 26);
        Console.WriteLine($"loaded {dune.Title}: price {dune.ListPrice}, stock {dune.Stock!.OnHand}, version {dune.Version}");

        _showCommands = true;
        Console.WriteLine("change only the price:");
        dune.ListPrice = 399.90m;
        await db.SaveChangesAsync();

        Console.WriteLine("a new review and the price back, in one SaveChanges:");
        Review review = new()
        {
            Id = ObjectId.GenerateNewId(),
            BookId = 26,
            Customer = new CustomerRef { CustomerId = 3, Name = "Grace Hopper" },
            Rating = 5,
            ReviewedAt = DateTime.UtcNow
        };
        db.Reviews.Add(review);
        dune.ListPrice = 390.50m;
        await db.SaveChangesAsync();
        _showCommands = false;

        // Undo: remove the review again
        db.Reviews.Remove(review);
        await db.SaveChangesAsync();
    }

    public static async Task QueryAsync(IServiceProvider services)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        BookstoreDbContext db = scope.ServiceProvider.GetRequiredService<BookstoreDbContext>();
        _showCommands = true;

        Console.WriteLine("Where + OrderBy + Take:");
        List<Book> scienceFiction = await db.Books
            .Where(b => b.Category!.Name == "Science Fiction" && b.ListPrice < 500m)
            .OrderByDescending(b => b.ListPrice)
            .Take(3)
            .ToListAsync();
        Console.WriteLine("  " + string.Join(", ", scienceFiction.Select(b => $"{b.Title} {b.ListPrice}")));

        Console.WriteLine("Select into an anonymous type:");
        var prices = await db.Books.Where(b => b.Id == 26).Select(b => new { b.Title, b.ListPrice }).ToListAsync();
        Console.WriteLine("  " + string.Join(", ", prices));

        Console.WriteLine("Include across collections:");
        Book dune = await db.Books.Include(b => b.Reviews).SingleAsync(b => b.Id == 26);
        Console.WriteLine($"  {dune.Title}: {dune.Reviews.Count} reviews");

        Console.WriteLine("GroupBy:");
        try
        {
            var byStatus = await db.Orders.GroupBy(o => o.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync();
            Console.WriteLine("  " + string.Join(", ", byStatus));
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"  {ex.GetType().Name}: {ex.Message.Split('\n')[0]} ...");
        }

        Console.WriteLine("ExecuteUpdate:");
        int updated = await db.Books.Where(b => b.Id == 26).ExecuteUpdateAsync(s => s.SetProperty(b => b.ListPrice, 390.50m));
        Console.WriteLine($"  {updated} book(s) updated");
        _showCommands = false;
    }

    public static async Task ConcurrencyAsync(IServiceProvider services)
    {
        IMongoCollection<BsonDocument> rawBooks = services.GetRequiredService<IMongoDatabase>().GetCollection<BsonDocument>("books");

        foreach (bool bumpVersion in new[] { false, true })
        {
            await using AsyncServiceScope scope = services.CreateAsyncScope();
            BookstoreDbContext db = scope.ServiceProvider.GetRequiredService<BookstoreDbContext>();

            // An editor opens Dune in the admin screen (EF Core)
            Book dune = await db.Books.SingleAsync(b => b.Id == 26);
            Console.WriteLine($"editor loads Dune: price {dune.ListPrice}, stock {dune.Stock!.OnHand}, version {dune.Version}");

            // Meanwhile an order is placed through the driver (post 3): conditional $inc on the stock
            UpdateDefinition<BsonDocument> sell = Builders<BsonDocument>.Update.Inc("stock.onHand", -2);
            if (bumpVersion)
            {
                sell = sell.Inc("version", 1L);   // the driver side plays by the row version rule too
            }
            await rawBooks.UpdateOneAsync(new BsonDocument { { "_id", 26 }, { "stock.onHand", new BsonDocument("$gte", 2) } }, sell);
            Console.WriteLine(bumpVersion ? "order sells 2 copies and increments version" : "order sells 2 copies");

            // The editor changes only the price and saves
            _showCommands = true;
            dune.ListPrice = 399.90m;
            try
            {
                await db.SaveChangesAsync();
                Console.WriteLine("editor saved the new price");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                Console.WriteLine($"{ex.GetType().Name}: {ex.Message}");
            }
            _showCommands = false;

            BsonDocument after = await rawBooks.Find(new BsonDocument("_id", 26)).FirstAsync();
            Console.WriteLine($"in the database: price {after["listPrice"]}, stock {after["stock"]["onHand"]}, version {after["version"]}");
            Console.WriteLine();

            // Undo for the next round
            await rawBooks.UpdateOneAsync(new BsonDocument("_id", 26), Builders<BsonDocument>.Update
                .Set("listPrice", new Decimal128(390.50m)).Set("stock.onHand", 51).Set("version", 0L));
        }
    }
}
