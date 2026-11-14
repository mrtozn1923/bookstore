using StackExchange.Redis;

namespace Bookstore.Api;

public sealed record ViewedBookRequest(string Isbn);

public static class RecentlyViewedEndpoints
{
    private const int MaxItems = 10;
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(90);

    public static IEndpointRouteBuilder MapRecentlyViewed(this IEndpointRouteBuilder app)
    {
        app.MapPost("/customers/{customerId:int}/recently-viewed", async (int customerId, ViewedBookRequest request, IConnectionMultiplexer redis) =>
        {
            IDatabase db = redis.GetDatabase();
            RedisKey key = RedisKeys.RecentlyViewed(customerId);

            // Remove the book if already there, push it to the head, keep only the newest 10 (pipelined, not atomic)
            await Task.WhenAll(
                db.ListRemoveAsync(key, request.Isbn),               // LREM key 0 isbn
                db.ListLeftPushAsync(key, request.Isbn),             // LPUSH
                db.ListTrimAsync(key, 0, MaxItems - 1),              // LTRIM key 0 9
                db.KeyExpireAsync(key, Lifetime));
            return Results.NoContent();
        });

        app.MapGet("/customers/{customerId:int}/recently-viewed", async (int customerId, IConnectionMultiplexer redis) =>
        {
            RedisValue[] isbns = await redis.GetDatabase().ListRangeAsync(RedisKeys.RecentlyViewed(customerId), 0, MaxItems - 1);
            return Results.Ok(isbns.Select(i => i.ToString()));
        });

        return app;
    }
}
