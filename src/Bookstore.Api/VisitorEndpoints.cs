using StackExchange.Redis;

namespace Bookstore.Api;

public sealed record VisitRequest(int CustomerId);

public static class VisitorEndpoints
{
    public static IEndpointRouteBuilder MapVisitors(this IEndpointRouteBuilder app)
    {
        app.MapPost("/visits", async (VisitRequest request, IConnectionMultiplexer redis, TimeProvider clock) =>
        {
            IDatabase db = redis.GetDatabase();
            RedisKey today = RedisKeys.DailyVisitors(DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime));
            Task<bool> added = db.SetAddAsync(today, request.CustomerId);
            Task<bool> ttl = db.KeyExpireAsync(today, TimeSpan.FromDays(8), ExpireWhen.HasNoExpiry);
            await Task.WhenAll(added, ttl);
            return Results.Ok(new { firstVisitToday = added.Result });
        });

        // Unique visitors over the last 7 days without building the union: SUNIONCARD (Redis 8.10+)
        app.MapGet("/visits/weekly", async (IConnectionMultiplexer redis, TimeProvider clock) =>
        {
            DateOnly today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
            RedisKey[] days = [.. Enumerable.Range(0, 7).Select(i => RedisKeys.DailyVisitors(today.AddDays(-i)))];
            long unique = await redis.GetDatabase().SetCombineLengthAsync(SetOperation.Union, days);
            return Results.Ok(new { from = today.AddDays(-6), to = today, uniqueVisitors = unique });
        });

        return app;
    }
}
