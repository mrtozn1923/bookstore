using StackExchange.Redis;

namespace Bookstore.Api;

public sealed record VisitRequest(int CustomerId);

public static class VisitorEndpoints
{
    private static readonly TimeSpan Retention = TimeSpan.FromDays(8);

    public static IEndpointRouteBuilder MapVisitors(this IEndpointRouteBuilder app)
    {
        // One visit, three structures: exact set (05), bitmap per customer id and HyperLogLog (07)
        app.MapPost("/visits", async (VisitRequest request, IConnectionMultiplexer redis, TimeProvider clock) =>
        {
            IDatabase db = redis.GetDatabase();
            DateOnly day = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
            RedisKey set = RedisKeys.DailyVisitors(day), bitmap = RedisKeys.ActiveCustomers(day), hll = RedisKeys.DailyVisitorsHll(day);

            Task<bool> added = db.SetAddAsync(set, request.CustomerId);                     // SADD
            Task<bool> wasActive = db.StringSetBitAsync(bitmap, request.CustomerId, true);   // SETBIT: returns the old bit
            Task<bool> hllChanged = db.HyperLogLogAddAsync(hll, request.CustomerId);         // PFADD
            Task[] ttl = [.. new[] { set, bitmap, hll }.Select(k => db.KeyExpireAsync(k, Retention, ExpireWhen.HasNoExpiry))];
            await Task.WhenAll([added, wasActive, hllChanged, .. ttl]);
            return Results.Ok(new { firstVisitToday = added.Result, wasActiveBefore = wasActive.Result });
        });

        // Unique visitors over the last 7 days: exact with SUNIONCARD (Redis 8.10+), estimated with PFCOUNT
        app.MapGet("/visits/weekly", async (IConnectionMultiplexer redis, TimeProvider clock) =>
        {
            IDatabase db = redis.GetDatabase();
            DateOnly today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
            DateOnly[] days = [.. Enumerable.Range(0, 7).Select(i => today.AddDays(-i))];

            Task<long> exact = db.SetCombineLengthAsync(SetOperation.Union, [.. days.Select(RedisKeys.DailyVisitors)]);
            Task<long> estimated = db.HyperLogLogLengthAsync([.. days.Select(RedisKeys.DailyVisitorsHll)]); // PFCOUNT k1 ... k7
            await Task.WhenAll(exact, estimated);
            return Results.Ok(new { from = today.AddDays(-6), to = today, uniqueVisitors = exact.Result, estimatedUniqueVisitors = estimated.Result });
        });

        // Customers active on a given day: BITCOUNT over the day's bitmap
        app.MapGet("/visits/{day}/active", async (DateOnly day, IConnectionMultiplexer redis) =>
        {
            long active = await redis.GetDatabase().StringBitCountAsync(RedisKeys.ActiveCustomers(day));
            return Results.Ok(new { day, activeCustomers = active });
        });

        return app;
    }
}
