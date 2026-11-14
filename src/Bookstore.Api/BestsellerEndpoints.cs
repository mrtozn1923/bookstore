using StackExchange.Redis;

namespace Bookstore.Api;

public static class BestsellerEndpoints
{
    private static readonly TimeSpan SnapshotLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan RecentOrdersKept = TimeSpan.FromHours(1);

    public static IEndpointRouteBuilder MapBestsellers(this IEndpointRouteBuilder app)
    {
        // Today's live list: rank-based paging, highest score first
        app.MapGet("/bestsellers/today", async (IConnectionMultiplexer redis, TimeProvider clock, int page = 1, int pageSize = 10) =>
        {
            RedisKey key = RedisKeys.Bestsellers(DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime));
            long start = (page - 1) * pageSize;
            SortedSetEntry[] entries = await redis.GetDatabase()
                .SortedSetRangeByRankWithScoresAsync(key, start, start + pageSize - 1, Order.Descending);
            return Results.Ok(entries.Select((e, i) => new { rank = start + i + 1, isbn = e.Element.ToString(), sold = e.Score }));
        });

        // The last 7 days: computed once into a snapshot, so that pages do not shift between requests
        app.MapGet("/bestsellers/week", async (IConnectionMultiplexer redis, TimeProvider clock, int page = 1, int pageSize = 10) =>
        {
            IDatabase db = redis.GetDatabase();
            DateOnly today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
            RedisKey week = RedisKeys.BestsellersWeek(today);

            if (!await db.KeyExistsAsync(week))
            {
                await BuildWeekAsync(db, today, week);
            }

            long start = (page - 1) * pageSize;
            SortedSetEntry[] entries = await db.SortedSetRangeByRankWithScoresAsync(week, start, start + pageSize - 1, Order.Descending);
            return Results.Ok(entries.Select((e, i) => new
            {
                rank = start + i + 1,
                isbn = e.Element.ToString(),
                sold = Math.Floor(e.Score / 10),
                daysWithSales = e.Score % 10
            }));
        });

        // Orders in the last N minutes: timestamp as score, old entries trimmed on every read
        app.MapGet("/orders/recent", async (IConnectionMultiplexer redis, TimeProvider clock, int minutes = 5) =>
        {
            IDatabase db = redis.GetDatabase();
            DateTimeOffset now = clock.GetUtcNow();
            long windowStart = now.AddMinutes(-minutes).ToUnixTimeMilliseconds();
            long keepFrom = now.Add(-RecentOrdersKept).ToUnixTimeMilliseconds();

            Task<long> trimmed = db.SortedSetRemoveRangeByScoreAsync(RedisKeys.RecentOrders,
                double.NegativeInfinity, keepFrom, Exclude.Stop);                                  // ZREMRANGEBYSCORE key -inf (keepFrom
            Task<long> count = db.SortedSetLengthAsync(RedisKeys.RecentOrders, windowStart);       // ZCOUNT key windowStart +inf
            await Task.WhenAll(trimmed, count);
            return Results.Ok(new { minutes, orders = count.Result });
        });

        return app;
    }

    // score = copies sold * 10 + days with sales (0-7): equal sales are ordered by how steadily the book sold
    private static async Task BuildWeekAsync(IDatabase db, DateOnly today, RedisKey week)
    {
        RedisKey[] days = [.. Enumerable.Range(0, 7).Select(i => RedisKeys.Bestsellers(today.AddDays(-i)))];
        string buildId = Guid.NewGuid().ToString("N");                 // own temporary keys per build
        RedisKey sold = RedisKeys.BestsellersWeekPart(today, "sold", buildId);
        RedisKey daysWithSales = RedisKeys.BestsellersWeekPart(today, "days", buildId);

        await Task.WhenAll(
            db.SortedSetCombineAndStoreAsync(SetOperation.Union, sold, days),                                 // ZUNIONSTORE (SUM)
            db.SortedSetCombineAndStoreAsync(SetOperation.Union, daysWithSales, days, null, Aggregate.Count)); // AGGREGATE COUNT (8.8+)
        await db.SortedSetCombineAndStoreAsync(SetOperation.Union, week, [sold, daysWithSales], [10, 1]);     // WEIGHTS 10 1
        await Task.WhenAll(
            db.KeyDeleteAsync([sold, daysWithSales]),
            db.KeyExpireAsync(week, SnapshotLifetime));
    }
}
