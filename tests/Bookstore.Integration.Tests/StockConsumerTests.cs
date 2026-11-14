using Bookstore.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;

namespace Bookstore.Integration.Tests;

public sealed class StockConsumerTests(RedisFixture fixture)
{
    [Fact]
    public async Task Moves_a_poison_message_to_the_dead_letter_stream_after_three_deliveries()
    {
        IDatabase db = fixture.Redis.GetDatabase();
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Worker:ConsumerName"] = "test" })
            .Build();
        StockConsumer consumer = new(fixture.Redis, config, NullLogger<StockConsumer>.Instance);
        await consumer.StartAsync(TestContext.Current.CancellationToken);

        RedisValue poisonId = await db.StreamAddAsync(OrderEventsConsumer.Stream,
            [new("type", "order-created"), new("orderId", 9999), new("isbn", "9780451524935"), new("quantity", "two")]);

        StreamEntry[] dead = [];
        for (int i = 0; i < 50 && dead.Length == 0; i++)
        {
            await Task.Delay(200, TestContext.Current.CancellationToken);
            dead = await db.StreamRangeAsync(OrderEventsConsumer.DeadLetters);
        }
        await consumer.StopAsync(TestContext.Current.CancellationToken);

        StreamEntry entry = Assert.Single(dead);
        Assert.Equal(poisonId, entry["sourceId"]);
        Assert.Equal("3", entry["deliveries"]);
        Assert.Equal(0, (await db.StreamPendingAsync(OrderEventsConsumer.Stream, "stock")).PendingMessageCount);
    }
}
