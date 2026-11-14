using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace Bookstore.Api;

public sealed record CreateOrderRequest(int CustomerId, string Isbn, int Quantity);

public static class OrderEndpoints
{
    private const string Processing = "processing";
    private static readonly TimeSpan IdempotencyWindow = TimeSpan.FromHours(24);

    public static IEndpointRouteBuilder MapOrders(this IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", async (CreateOrderRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            IConnectionMultiplexer redis, TimeProvider clock) =>
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                return Results.BadRequest(new { error = "Idempotency-Key header is required" });
            }

            IDatabase db = redis.GetDatabase();
            RedisKey key = RedisKeys.Idempotency(idempotencyKey);

            // SET key processing NX EX 86400: only the first request gets through
            bool first = await db.StringSetAsync(key, Processing, IdempotencyWindow, When.NotExists);
            if (!first)
            {
                RedisValue stored = await db.StringGetAsync(key);
                return stored == Processing
                    ? Results.Conflict(new { error = "a request with this key is still being processed" })
                    : Results.Ok(new { orderId = (long)stored, replayed = true });
            }

            try
            {
                long orderId = await db.StringIncrementAsync(RedisKeys.OrderIdSequence); // stands in for the real order insert
                // SET key <orderId> IFEQ processing KEEPTTL: record the result only if we still own the key
                await db.StringSetAsync(key, orderId, Expiration.KeepTtl, ValueCondition.Equal(Processing));
                await RecordSaleAsync(db, request, orderId, clock.GetUtcNow());
                await PublishOrderCreatedAsync(db, request, orderId, idempotencyKey);
                return Results.Created($"/orders/{orderId}", new { orderId, replayed = false });
            }
            catch
            {
                // Release the key so that the client can retry; only if it is still ours
                await db.StringDeleteAsync(key, ValueCondition.Equal(Processing));
                throw;
            }
        });

        return app;
    }

    // XADD bookstore:orders:events IDMP bookstore-api <Idempotency-Key> MAXLEN ~ 100000 * ...
    // A retried XADD with the same idempotency key returns the first entry's id instead of a duplicate (Redis 8.6+)
    private static Task<RedisValue> PublishOrderCreatedAsync(IDatabase db, CreateOrderRequest request, long orderId, string idempotencyKey) =>
        db.StreamAddAsync(RedisKeys.OrderEvents,
            [
                new("type", "order-created"),
                new("orderId", orderId),
                new("customerId", request.CustomerId),
                new("isbn", request.Isbn),
                new("quantity", request.Quantity)
            ],
            new StreamIdempotentId("bookstore-api", idempotencyKey), maxLength: 100_000, useApproximateMaxLength: true);

    // Daily bestsellers (ZINCRBY) and the time-ordered recent orders (timestamp as score)
    private static async Task RecordSaleAsync(IDatabase db, CreateOrderRequest request, long orderId, DateTimeOffset now)
    {
        RedisKey daily = RedisKeys.Bestsellers(DateOnly.FromDateTime(now.UtcDateTime));
        await Task.WhenAll(
            db.SortedSetIncrementAsync(daily, request.Isbn, request.Quantity),                       // ZINCRBY
            db.KeyExpireAsync(daily, TimeSpan.FromDays(8), ExpireWhen.HasNoExpiry),                  // EXPIRE ... NX
            db.SortedSetAddAsync(RedisKeys.RecentOrders, orderId, now.ToUnixTimeMilliseconds()));    // ZADD
    }
}
