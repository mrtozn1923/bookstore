using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Servers;

namespace Bookstore.MongoLab;

public static class ReplicaSetLab
{
    // Writes to order 379 every 100 ms and reports slow or failed writes and primary changes
    public static async Task WatchWritesAsync(IServiceProvider services, TimeSpan duration)
    {
        IMongoClient client = services.GetRequiredService<IMongoClient>();
        IMongoCollection<BsonDocument> orders = services.GetRequiredService<IMongoDatabase>().GetCollection<BsonDocument>("orders");
        FilterDefinition<BsonDocument> order379 = new BsonDocument("_id", 379);
        UpdateDefinition<BsonDocument> touch = Builders<BsonDocument>.Update.Inc("failoverCheck", 1);

        Stopwatch clock = Stopwatch.StartNew();
        string? lastPrimary = null;
        int succeeded = 0, failed = 0;
        long slowest = 0;
        while (clock.Elapsed < duration)
        {
            Stopwatch call = Stopwatch.StartNew();
            string result = "ok";
            try
            {
                await orders.UpdateOneAsync(order379, touch);
                succeeded++;
            }
            catch (Exception ex)
            {
                result = $"{ex.GetType().Name}: {ex.Message.Split(" using ")[0]}";
                failed++;
            }
            call.Stop();
            slowest = Math.Max(slowest, call.ElapsedMilliseconds);

            string primary = client.Cluster.Description.Servers
                .FirstOrDefault(s => s.Type == ServerType.ReplicaSetPrimary)?.EndPoint is DnsEndPoint endPoint
                    ? $"{endPoint.Host}:{endPoint.Port}"
                    : "none";
            if (primary != lastPrimary || call.ElapsedMilliseconds > 500 || result != "ok")
            {
                Console.WriteLine($"{clock.Elapsed.TotalSeconds,6:0.0}s  primary {primary,-16} write {call.ElapsedMilliseconds,5} ms  {result}");
                lastPrimary = primary;
            }
            await Task.Delay(100);
        }
        Console.WriteLine($"writes ok {succeeded}, failed {failed}, slowest {slowest} ms");
        await orders.UpdateOneAsync(order379, Builders<BsonDocument>.Update.Unset("failoverCheck"));
    }
}
