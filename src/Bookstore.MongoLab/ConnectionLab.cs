using System.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Bookstore.MongoLab;

public static class ConnectionLab
{
    public static async Task PingAsync(string connectionString)
    {
        MongoClientSettings settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
        MongoClient client = new(settings);

        Stopwatch watch = Stopwatch.StartNew();
        try
        {
            IMongoDatabase database = client.GetDatabase("bookstore");
            await database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
            long books = await database.GetCollection<BsonDocument>("books")
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);

            Console.WriteLine($"ping ok in {watch.ElapsedMilliseconds} ms, books = {books}");
            foreach (var server in client.Cluster.Description.Servers)
            {
                Console.WriteLine($"  {server.EndPoint} -> {server.Type}");
            }
        }
        catch (TimeoutException ex)
        {
            Console.WriteLine($"{ex.GetType().Name} after {watch.ElapsedMilliseconds} ms");
            Console.WriteLine(ex.Message);
        }
    }
}
