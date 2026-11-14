using StackExchange.Redis;

namespace Bookstore.Integration.Tests;

public sealed class ReserveStockTests(RedisFixture fixture)
{
    private static Task<RedisResult> ReserveAsync(IDatabase db, string isbn, string orderId, int quantity) =>
        db.ExecuteAsync("FCALL", "reserve_stock", 2, $"bookstore:stock:{{{isbn}}}", $"bookstore:stock:{{{isbn}}}:reservations",
            orderId, quantity);

    [Fact]
    public async Task Sells_exactly_the_stock_when_200_buyers_race_for_100_copies()
    {
        IDatabase db = fixture.Redis.GetDatabase();
        await db.StringSetAsync("bookstore:stock:{9780441013593}", 100);

        RedisResult[] results = await Task.WhenAll(Enumerable.Range(1, 200)
            .Select(i => ReserveAsync(db, "9780441013593", $"order-{i}", 1)));

        Assert.Equal(100, results.Count(r => (long)r >= 0));
        Assert.Equal(100, results.Count(r => (long)r == -1));
        Assert.Equal(0, (long)await db.StringGetAsync("bookstore:stock:{9780441013593}"));
        Assert.Equal(100, await db.HashLengthAsync("bookstore:stock:{9780441013593}:reservations"));
    }

    [Fact]
    public async Task Does_not_reserve_the_same_order_twice()
    {
        IDatabase db = fixture.Redis.GetDatabase();
        await db.StringSetAsync("bookstore:stock:{9780141439518}", 5);

        Assert.Equal(3, (long)await ReserveAsync(db, "9780141439518", "order-1001", 2));
        Assert.Equal(-2, (long)await ReserveAsync(db, "9780141439518", "order-1001", 2));
        Assert.Equal(3, (long)await db.StringGetAsync("bookstore:stock:{9780141439518}"));
    }
}
