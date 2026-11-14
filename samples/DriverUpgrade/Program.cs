using System.Globalization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
string connectionString = Environment.GetEnvironmentVariable("BOOKSTORE_MONGO") ?? "mongodb://localhost:27017";
string driver = typeof(MongoClient).Assembly.GetName().Version!.ToString(3);

#if DRIVER2
// Driver 2.x setup, as many projects have it
BsonDefaults.GuidRepresentationMode = GuidRepresentationMode.V3;
MongoClientSettings settings = MongoClientSettings.FromConnectionString(connectionString);
settings.LinqProvider = LinqProvider.V3;
#else
// Driver 3.x: V3 Guid mode and LINQ3 are the only options, the settings are gone
MongoClientSettings settings = MongoClientSettings.FromConnectionString(connectionString);
#endif
BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

IMongoCollection<PricedBook> books = new MongoClient(settings).GetDatabase("bookstore").GetCollection<PricedBook>("driver_upgrade");

if (args.FirstOrDefault() == "seed")
{
    await books.Database.DropCollectionAsync("driver_upgrade");
    await books.InsertManyAsync(
    [
        new PricedBook { Id = 26, Title = "Dune", ListPrice = 390.50m, ListedAt = new DateTimeOffset(2024, 6, 1, 9, 0, 0, TimeSpan.FromHours(3)) },
        new PricedBook { Id = 35, Title = "Nineteen Eighty-Four", ListPrice = 530.90m, ListedAt = new DateTimeOffset(2024, 6, 1, 9, 0, 0, TimeSpan.FromHours(3)) },
        new PricedBook { Id = 77, Title = "The Art of Computer Programming", ListPrice = 1450.00m, ListedAt = new DateTimeOffset(2024, 6, 1, 9, 0, 0, TimeSpan.FromHours(3)) }
    ]);
    Console.WriteLine($"driver {driver}: seeded 3 books");
}

// The same LINQ query under both drivers
#if DRIVER2
IMongoQueryable<PricedBook> cheap = books.AsQueryable().Where(b => b.ListPrice < 500m);
#else
IQueryable<PricedBook> cheap = books.AsQueryable().Where(b => b.ListPrice < 500m);
#endif
Console.WriteLine($"driver {driver}: {cheap}");
Console.WriteLine($"driver {driver}: books under 500: {string.Join(", ", (await cheap.ToListAsync()).Select(b => $"{b.Title} {b.ListPrice} (listed {b.ListedAt:yyyy-MM-dd HH:mm zzz})"))}");

BsonDocument raw = await books.Database.GetCollection<BsonDocument>("driver_upgrade").Find(new BsonDocument("_id", 26)).FirstAsync();
Console.WriteLine($"driver {driver}: stored types: ListPrice {raw["ListPrice"].BsonType}, ListedAt {raw["ListedAt"].BsonType}");

public sealed class PricedBook
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public decimal ListPrice { get; set; }
    public DateTimeOffset ListedAt { get; set; }
}
