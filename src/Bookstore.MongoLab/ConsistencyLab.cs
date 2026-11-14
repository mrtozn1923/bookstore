using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Bookstore.MongoLab;

public static class ConsistencyLab
{
    private const int Rounds = 200;

    // Write a value to order 379 on the primary, read it back from a secondary at once, count stale reads
    public static async Task ReadYourWritesAsync(IServiceProvider services)
    {
        IMongoClient client = services.GetRequiredService<IMongoClient>();
        IMongoCollection<BsonDocument> orders = services.GetRequiredService<IMongoDatabase>().GetCollection<BsonDocument>("orders");

        await RunAsync("w:1, secondary", orders.WithWriteConcern(WriteConcern.W1), orders, session: null);
        await RunAsync("w:majority, secondary", orders.WithWriteConcern(WriteConcern.WMajority), orders, session: null);

        using IClientSessionHandle session = await client.StartSessionAsync(new ClientSessionOptions { CausalConsistency = true });
        await RunAsync("w:majority, secondary, causal session",
            orders.WithWriteConcern(WriteConcern.WMajority), orders.WithReadConcern(ReadConcern.Majority), session);

        await orders.UpdateOneAsync(new BsonDocument("_id", 379), Builders<BsonDocument>.Update.Unset("readCheck"));
    }

    private static async Task RunAsync(string label, IMongoCollection<BsonDocument> writer, IMongoCollection<BsonDocument> reader,
        IClientSessionHandle? session)
    {
        IMongoCollection<BsonDocument> secondary = reader.WithReadPreference(ReadPreference.Secondary);
        FilterDefinition<BsonDocument> order379 = new BsonDocument("_id", 379);
        int stale = 0;
        for (int i = 1; i <= Rounds; i++)
        {
            UpdateDefinition<BsonDocument> update = Builders<BsonDocument>.Update.Set("readCheck", i);
            BsonDocument read;
            if (session is null)
            {
                await writer.UpdateOneAsync(order379, update);
                read = await secondary.Find(order379).FirstAsync();
            }
            else
            {
                await writer.UpdateOneAsync(session, order379, update);
                read = await secondary.Find(session, order379).FirstAsync();
            }
            if (read.GetValue("readCheck", 0).AsInt32 != i)
            {
                stale++;
            }
        }
        Console.WriteLine($"{label,-40} stale reads: {stale} of {Rounds}");
    }
}
