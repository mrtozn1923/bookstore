using StackExchange.Redis;
using Testcontainers.Redis;

[assembly: AssemblyFixture(typeof(Bookstore.Integration.Tests.RedisFixture))]

namespace Bookstore.Integration.Tests;

// One real Redis 8 container for the whole test run, with the bookstore Functions library loaded
public sealed class RedisFixture : IAsyncLifetime
{
    private readonly RedisContainer container = new RedisBuilder("redis:8.10.3").Build();

    public IConnectionMultiplexer Redis { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await container.StartAsync();
        Redis = await ConnectionMultiplexer.ConnectAsync(container.GetConnectionString());
        string library = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "bookstore.lua"));
        await Redis.GetDatabase().ExecuteAsync("FUNCTION", "LOAD", "REPLACE", library);
    }

    public async ValueTask DisposeAsync()
    {
        await Redis.DisposeAsync();
        await container.DisposeAsync();
    }
}
