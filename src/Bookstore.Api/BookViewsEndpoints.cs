using StackExchange.Redis;

namespace Bookstore.Api;

public static class BookViewsEndpoints
{
    public static IEndpointRouteBuilder MapBookViews(this IEndpointRouteBuilder app)
    {
        app.MapPost("/books/{bookId:int}/views", async (int bookId, IConnectionMultiplexer redis, TimeProvider clock) =>
        {
            IDatabase db = redis.GetDatabase();
            RedisKey daily = RedisKeys.BookViewsDaily(bookId, DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime));

            // Three commands, sent together (pipelined) and awaited together
            Task<long> total = db.StringIncrementAsync(RedisKeys.BookViews(bookId));
            Task<long> today = db.StringIncrementAsync(daily);
            Task<bool> ttl = db.KeyExpireAsync(daily, TimeSpan.FromDays(8), ExpireWhen.HasNoExpiry); // EXPIRE ... NX
            await Task.WhenAll(total, today, ttl);

            return Results.Ok(new { bookId, views = total.Result, today = today.Result });
        });

        app.MapGet("/books/{bookId:int}/views", async (int bookId, IConnectionMultiplexer redis) =>
        {
            RedisValue views = await redis.GetDatabase().StringGetAsync(RedisKeys.BookViews(bookId));
            return Results.Ok(new { bookId, views = views.IsNull ? 0 : (long)views });
        });

        return app;
    }
}
