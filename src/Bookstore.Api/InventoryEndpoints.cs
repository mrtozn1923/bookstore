using StackExchange.Redis;

namespace Bookstore.Api;

public sealed record StockRequest(long Quantity);

public sealed record ReservationRequest(string OrderId, int Quantity);

public static class InventoryEndpoints
{
    // Check the stock and reserve in one atomic step; the same order is never reserved twice
    private static readonly LuaScript Reserve = LuaScript.Prepare("""
        if redis.call('HEXISTS', @reservationsKey, @orderId) == 1 then
            return -2
        end
        local stock = tonumber(redis.call('GET', @stockKey) or '0')
        if stock < tonumber(@quantity) then
            return -1
        end
        redis.call('HSET', @reservationsKey, @orderId, @quantity)
        return redis.call('DECRBY', @stockKey, @quantity)
        """);

    public static IEndpointRouteBuilder MapInventory(this IEndpointRouteBuilder app)
    {
        app.MapPut("/inventory/{isbn}/stock", async (string isbn, StockRequest request, IConnectionMultiplexer redis) =>
        {
            await redis.GetDatabase().StringSetAsync(RedisKeys.Stock(isbn), request.Quantity);
            return Results.Ok(new { isbn, stock = request.Quantity });
        });

        app.MapPost("/inventory/{isbn}/reservations", async (string isbn, ReservationRequest request, IConnectionMultiplexer redis) =>
        {
            RedisResult result = await Reserve.EvaluateAsync(redis.GetDatabase(), new
            {
                stockKey = RedisKeys.Stock(isbn),
                reservationsKey = RedisKeys.StockReservations(isbn),
                orderId = request.OrderId,
                quantity = request.Quantity
            });
            long left = (long)result;
            return left switch
            {
                -1 => Results.Conflict(new { error = "not enough stock" }),
                -2 => Results.Ok(new { request.OrderId, alreadyReserved = true }),
                _ => Results.Created($"/inventory/{isbn}/reservations/{request.OrderId}", new { request.OrderId, stockLeft = left })
            };
        });

        return app;
    }
}
