using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Bookstore.MongoLab;

public static class DriverLab
{
    // Guess first: a Guid key without any serializer configuration
    public static async Task GuidWithoutSetupAsync(string connectionString)
    {
        IMongoCollection<StockReservation> reservations = new MongoClient(connectionString)
            .GetDatabase("bookstore").GetCollection<StockReservation>("stock_reservations");
        try
        {
            await reservations.InsertOneAsync(new StockReservation(Guid.NewGuid(), 26, 380, 1, DateTime.UtcNow.AddMinutes(15)));
            Console.WriteLine("inserted");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"  inner {ex.InnerException?.GetType().Name}: {ex.InnerException?.Message}");
        }
    }

    public static async Task MappingAsync(IServiceProvider services)
    {
        IMongoCollection<Book> books = services.GetRequiredService<IMongoCollection<Book>>();
        Book dune = await books.Find(b => b.Id == 26).FirstAsync();
        Console.WriteLine($"{dune.Title} by {dune.Authors[0].Name}, {dune.Category?.Name}");
        Console.WriteLine($"  ListPrice {dune.ListPrice} ({dune.ListPrice.GetType().Name})");
        Console.WriteLine($"  ListedOn  {dune.ListedOn:O} (Kind = {dune.ListedOn.Kind})");
        Console.WriteLine($"  Stock     {dune.Stock?.OnHand} on hand");

        // Write a sample with several .NET types and read it back as raw BSON
        IMongoDatabase database = services.GetRequiredService<IMongoDatabase>();
        IMongoCollection<TypeSample> samples = database.GetCollection<TypeSample>("type_samples");
        await samples.DeleteManyAsync(FilterDefinition<TypeSample>.Empty);
        await samples.InsertOneAsync(new TypeSample(
            1, 390.50m,
            new DateTime(2026, 6, 30, 9, 0, 0, DateTimeKind.Utc),
            new DateTimeOffset(2026, 6, 30, 12, 0, 0, TimeSpan.FromHours(3)),
            Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e"),
            OrderStatus.Paid));

        BsonDocument raw = await database.GetCollection<BsonDocument>("type_samples").Find(new BsonDocument()).FirstAsync();
        foreach (BsonElement element in raw)
        {
            string subtype = element.Value is BsonBinaryData binary ? $" (subtype {binary.SubType})" : "";
            Console.WriteLine($"  {element.Name,-10} {element.Value.BsonType}{subtype}");
        }
        Console.WriteLine(raw.ToJson());
        await database.DropCollectionAsync("type_samples");
    }

    public static async Task QueryAsync(IServiceProvider services)
    {
        IMongoCollection<Book> books = services.GetRequiredService<IMongoCollection<Book>>();

        // LINQ: translated to an aggregation pipeline and run on the server
        var query = books.AsQueryable()
            .Where(b => b.Category!.Name == "Science Fiction" && b.ListPrice < 500m)
            .OrderByDescending(b => b.ListPrice)
            .Select(b => new { b.Title, b.ListPrice })
            .Take(3);
        Console.WriteLine(query.ToString());
        foreach (var book in await query.ToListAsync())
        {
            Console.WriteLine($"  {book.Title,-22} {book.ListPrice}");
        }

        // Builders: the same kind of query, with a Turkish collation (post 6)
        FilterDefinition<Book> filter = Builders<Book>.Filter.Eq(b => b.Title, "ince memed");
        FindOptions options = new() { Collation = new Collation("tr", strength: CollationStrength.Secondary) };
        Book? found = await books.Find(filter, options)
            .Project<Book>(Builders<Book>.Projection.Include(b => b.Title).Include(b => b.Isbn).Include(b => b.Language))
            .FirstOrDefaultAsync();
        Console.WriteLine($"collation tr: {found?.Title}");

        // Typed, atomic update: decrement stock only if enough is left, get the new value back
        Book? updated = await books.FindOneAndUpdateAsync(
            Builders<Book>.Filter.Eq(b => b.Id, 26) & Builders<Book>.Filter.Gte(b => b.Stock!.OnHand, 2),
            Builders<Book>.Update.Inc(b => b.Stock!.OnHand, -2).CurrentDate(b => b.Stock!.UpdatedAt),
            new FindOneAndUpdateOptions<Book> { ReturnDocument = ReturnDocument.After });
        Console.WriteLine($"stock after buying 2: {updated?.Stock?.OnHand}");
        await books.UpdateOneAsync(b => b.Id == 26, Builders<Book>.Update.Inc(b => b.Stock!.OnHand, 2));

        // A projection that calls a .NET method cannot run on the server
        try
        {
            var labels = await books.AsQueryable()
                .Where(b => b.Id == 26)
                .Select(b => PriceLabel(b))
                .ToListAsync();
            Console.WriteLine(labels[0]);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static string PriceLabel(Book book) => $"{book.Title}: {book.ListPrice:0.00} TRY";

    public static async Task EditionsAsync(IServiceProvider services)
    {
        IMongoDatabase database = services.GetRequiredService<IMongoDatabase>();
        IMongoCollection<Edition> editions = database.GetCollection<Edition>("editions");
        await database.DropCollectionAsync("editions");
        await editions.InsertManyAsync(
        [
            new PrintEdition { Id = "26-print", BookId = 26, Price = 390.50m, Binding = "paperback", PageCount = 513 },
            new Ebook { Id = "26-ebook", BookId = 26, Price = 210.00m, FileFormats = ["epub", "pdf"] },
            new Audiobook { Id = "26-audio", BookId = 26, Price = 320.00m, Narrator = "Grace Hopper", DurationMinutes = 1263 }
        ]);

        await database.GetCollection<BsonDocument>("editions").Find(new BsonDocument())
            .Project(new BsonDocument { { "_t", 1 }, { "price", 1 } })
            .ForEachAsync(d => Console.WriteLine($"  {d}"));

        // OfType<T> adds a filter on _t; the result is typed
        List<Edition> all = await editions.Find(e => e.BookId == 26).SortBy(e => e.Price).ToListAsync();
        Console.WriteLine("read back: " + string.Join(", ", all.Select(e => e.GetType().Name)));
        Ebook ebook = await editions.OfType<Ebook>().Find(FilterDefinition<Ebook>.Empty).FirstAsync();
        Console.WriteLine($"ebook formats: {string.Join("/", ebook.FileFormats)}");
        await database.DropCollectionAsync("editions");
    }

    // Pitfall: data written by an old application with the legacy GUID format (subtype 3)
    public static async Task LegacyGuidAsync(IServiceProvider services)
    {
        IMongoDatabase database = services.GetRequiredService<IMongoDatabase>();
        Guid id = Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e");
        IMongoCollection<BsonDocument> raw = database.GetCollection<BsonDocument>("stock_reservations");
        await raw.DeleteManyAsync(new BsonDocument());
        await raw.InsertOneAsync(new BsonDocument
        {
            { "_id", new BsonBinaryData(id, GuidRepresentation.CSharpLegacy) },
            { "bookId", 26 }, { "orderId", 380 }, { "quantity", 1 }, { "expiresAt", new DateTime(2026, 6, 30, 10, 0, 0, DateTimeKind.Utc) }
        });

        IMongoCollection<StockReservation> reservations = database.GetCollection<StockReservation>("stock_reservations");
        try
        {
            StockReservation reservation = await reservations.Find(FilterDefinition<StockReservation>.Empty).FirstAsync();
            Console.WriteLine($"read {reservation.Id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.GetType().Name}: {ex.Message}");
        }
        long byStandard = await reservations.CountDocumentsAsync(r => r.Id == id);
        Console.WriteLine($"filter with the same Guid finds {byStandard} document(s)");
        await database.DropCollectionAsync("stock_reservations");
    }
}
