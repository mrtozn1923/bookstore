using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Bookstore.MongoLab;

public static class AdvancedLab
{
    public static async Task AggregateAsync(IServiceProvider services)
    {
        IMongoCollection<Order> orders = services.GetRequiredService<IMongoCollection<Order>>();
        DateTime from = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime to = new(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc);

        // Typed stages: Match and Group are translated from C# expressions
        var monthly = orders.Aggregate()
            .Match(o => o.Status != OrderStatus.Cancelled && o.OrderedAt >= from && o.OrderedAt < to)
            .Group(o => new { o.OrderedAt.Year, o.OrderedAt.Month }, g => new { g.Key.Year, g.Key.Month, Revenue = g.Sum(o => o.Total) })
            .SortBy(m => m.Year).ThenBy(m => m.Month);
        Console.WriteLine(monthly.ToString());

        // A stage the typed API does not cover: append it as BsonDocument
        List<BsonDocument> withYtd = await monthly
            .AppendStage<BsonDocument>(new BsonDocument("$setWindowFields", new BsonDocument
            {
                { "sortBy", new BsonDocument { { "Year", 1 }, { "Month", 1 } } },
                { "output", new BsonDocument("Ytd", new BsonDocument
                    {
                        { "$sum", "$Revenue" },
                        { "window", new BsonDocument("documents", new BsonArray { "unbounded", "current" }) }
                    }) }
            }))
            .ToListAsync();
        foreach (BsonDocument m in withYtd)
        {
            Console.WriteLine($"  {m["Year"]}-{m["Month"].AsInt32:00}  revenue {m["Revenue"],9}  ytd {m["Ytd"],9}");
        }
    }

    public static async Task BulkAsync(IServiceProvider services)
    {
        IMongoCollection<Book> books = services.GetRequiredService<IMongoCollection<Book>>();
        Book dune = await books.Find(b => b.Id == 26).FirstAsync();

        // A price list import: the second model is a duplicate _id and fails
        List<WriteModel<Book>> models =
        [
            new UpdateOneModel<Book>(Builders<Book>.Filter.Eq(b => b.Id, 26), Builders<Book>.Update.Set(b => b.ListPrice, 399.90m)),
            new InsertOneModel<Book>(dune),
            new UpdateOneModel<Book>(Builders<Book>.Filter.Eq(b => b.Id, 35), Builders<Book>.Update.Set(b => b.ListPrice, 529.90m))
        ];

        foreach (bool ordered in new[] { true, false })
        {
            await books.UpdateOneAsync(b => b.Id == 26, Builders<Book>.Update.Set(b => b.ListPrice, 390.50m));
            await books.UpdateOneAsync(b => b.Id == 35, Builders<Book>.Update.Set(b => b.ListPrice, 530.90m));
            try
            {
                await books.BulkWriteAsync(models, new BulkWriteOptions { IsOrdered = ordered });
            }
            catch (MongoBulkWriteException<Book> ex)
            {
                Console.WriteLine($"IsOrdered = {ordered,-5}: modified {ex.Result.ModifiedCount}, error at index {ex.WriteErrors[0].Index} " +
                    $"(code {ex.WriteErrors[0].Code}), not processed {ex.UnprocessedRequests.Count}");
            }
        }
        await books.UpdateOneAsync(b => b.Id == 26, Builders<Book>.Update.Set(b => b.ListPrice, 390.50m));
        await books.UpdateOneAsync(b => b.Id == 35, Builders<Book>.Update.Set(b => b.ListPrice, 530.90m));

        // MongoDB 8.0+: one bulkWrite command across several collections (not a transaction)
        IMongoClient client = services.GetRequiredService<IMongoClient>();
        Order order = SampleOrder(380, (26, 1));
        ClientBulkWriteResult clientResult = await client.BulkWriteAsync(
        [
            new BulkWriteInsertOneModel<Order>("bookstore.orders", order),
            new BulkWriteUpdateOneModel<Book>("bookstore.books",
                Builders<Book>.Filter.Eq(b => b.Id, 26), Builders<Book>.Update.Inc(b => b.Stock!.OnHand, -1))
        ]);
        Console.WriteLine($"client bulk write: inserted {clientResult.InsertedCount}, modified {clientResult.ModifiedCount}");
        await services.GetRequiredService<IMongoCollection<Order>>().DeleteOneAsync(o => o.Id == 380);
        await books.UpdateOneAsync(b => b.Id == 26, Builders<Book>.Update.Inc(b => b.Stock!.OnHand, 1));
    }

    public static async Task TransactionAsync(IServiceProvider services)
    {
        IMongoCollection<Book> books = services.GetRequiredService<IMongoCollection<Book>>();
        IMongoCollection<Order> orders = services.GetRequiredService<IMongoCollection<Order>>();
        OrderPlacement placement = ActivatorUtilities.CreateInstance<OrderPlacement>(services);
        await ResetAsync(books, orders);

        async Task ShowAsync(string label)
        {
            List<Book> both = await books.Find(b => b.Id == 26 || b.Id == 35).SortBy(b => b.Id).ToListAsync();
            long placed = await orders.CountDocumentsAsync(o => o.Id >= 380);
            Console.WriteLine($"{label,-28} Dune {both[0].Stock!.OnHand}, 1984 {both[1].Stock!.OnHand}, new orders {placed}");
        }

        await ShowAsync("start");
        await placement.PlaceAsync(SampleOrder(380, (26, 2), (35, 1)));
        await ShowAsync("order 380 (Dune 2, 1984 1)");
        try
        {
            await placement.PlaceAsync(SampleOrder(381, (26, 1), (35, 100)));
        }
        catch (InsufficientStockException ex)
        {
            Console.WriteLine($"order 381: {ex.Message}");
        }
        await ShowAsync("after order 381");

        // 20 customers buy Dune at the same moment: write conflicts are retried by WithTransactionAsync
        int before = placement.Attempts;
        await Task.WhenAll(Enumerable.Range(0, 20).Select(i => placement.PlaceAsync(SampleOrder(400 + i, (26, 1)))));
        await ShowAsync("20 concurrent orders");
        Console.WriteLine($"transaction bodies run: {placement.Attempts - before} for 20 orders");
        await ResetAsync(books, orders);
    }

    private static async Task ResetAsync(IMongoCollection<Book> books, IMongoCollection<Order> orders)
    {
        await orders.DeleteManyAsync(o => o.Id >= 380);
        await books.UpdateOneAsync(b => b.Id == 26, Builders<Book>.Update.Set(b => b.Stock!.OnHand, 51));
        await books.UpdateOneAsync(b => b.Id == 35, Builders<Book>.Update.Set(b => b.Stock!.OnHand, 52));
    }

    private static Order SampleOrder(int id, params (int BookId, int Quantity)[] items) => new()
    {
        Id = id,
        Customer = new CustomerRef(3, "Grace Hopper"),
        OrderedAt = new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc),
        Status = OrderStatus.Pending,
        Items = [.. items.Select(i => new OrderItem(i.BookId, i.BookId == 26 ? "Dune" : "Nineteen Eighty-Four", i.Quantity, 100m))],
        Total = items.Sum(i => i.Quantity * 100m),
        Currency = "TRY"
    };

    public static async Task WatchAsync(string connectionString)
    {
        IHost BuildHost()
        {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder();
            builder.Logging.ClearProviders();
            builder.Logging.AddSimpleConsole(o => o.SingleLine = true);
            builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
            builder.Services.AddBookstoreMongo(connectionString);
            builder.Services.AddSingleton<AuthorNameSync>();
            builder.Services.AddHostedService(sp => sp.GetRequiredService<AuthorNameSync>());
            return builder.Build();
        }

        using (IHost setup = BuildHost())
        {
            await ResetAuthorAsync(setup.Services);    // Frank Herbert, no saved resume token
        }

        using (IHost host = BuildHost())
        {
            await host.StartAsync();
            AuthorNameSync sync = host.Services.GetRequiredService<AuthorNameSync>();
            await sync.Watching;
            await SetFirstNameAsync(host.Services, "Frank P.");
            await WaitForAsync(() => sync.Processed == 1);
            await host.StopAsync();
        }

        // While the service is down the author changes again
        using (IHost offline = BuildHost())
        {
            await SetFirstNameAsync(offline.Services, "Franklin");
            Console.WriteLine("service stopped; author 18 renamed to Franklin");
        }

        using (IHost host = BuildHost())
        {
            await host.StartAsync();
            AuthorNameSync sync = host.Services.GetRequiredService<AuthorNameSync>();
            await WaitForAsync(() => sync.Processed == 1);
            await host.StopAsync();
            List<Book> books = await host.Services.GetRequiredService<IMongoCollection<Book>>()
                .Find(b => b.Authors.Any(a => a.AuthorId == 18)).SortBy(b => b.Id).ToListAsync();
            Console.WriteLine("books: " + string.Join(", ", books.Select(b => $"{b.Title} by {b.Authors[0].Name}")));
            await ResetAuthorAsync(host.Services);
        }
    }

    private static async Task ResetAuthorAsync(IServiceProvider services)
    {
        await SetFirstNameAsync(services, "Frank");
        await services.GetRequiredService<IMongoCollection<Book>>().UpdateManyAsync(
            b => b.Authors.Any(a => a.AuthorId == 18), Builders<Book>.Update.Set("authors.0.name", "Frank Herbert"));
        await services.GetRequiredService<IMongoDatabase>().DropCollectionAsync("resume_tokens");
    }

    private static Task SetFirstNameAsync(IServiceProvider services, string firstName) =>
        services.GetRequiredService<IMongoCollection<Author>>()
            .UpdateOneAsync(a => a.Id == 18, Builders<Author>.Update.Set(a => a.FirstName, firstName));

    private static async Task WaitForAsync(Func<bool> condition)
    {
        for (int i = 0; i < 200 && !condition(); i++)
        {
            await Task.Delay(50);
        }
    }
}
