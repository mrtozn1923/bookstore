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
            IConnectionMultiplexer redis) =>
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
}
