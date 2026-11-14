using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace Bookstore.MongoLab.Tests;

// One single-node replica set per test run: transactions and change streams need a replica set
public sealed class MongoFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongodb/mongodb-community-server:9.0-ubuntu2204")
        .WithReplicaSet("rs0")
        .Build();

    public IMongoClient Client { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        BookstoreMongo.RegisterSerialization();
        Client = new MongoClient(_container.GetConnectionString());
    }

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
