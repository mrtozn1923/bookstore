using StackExchange.Redis;

namespace Bookstore.Api;

public sealed record StockRequest(long Quantity);

public sealed record ReservationRequest(string OrderId, int Quantity);

public static class InventoryEndpoints
{
    // The reservation logic lives in the bookstore Functions library (lab/functions/bookstore.lua, post 08).
    // Post 14: the application user may call FCALL but not EVAL, so no Lua text is sent from here any more.
    public static async Task<bool> FunctionsLoadedAsync(IDatabase db)
    {
        RedisResult libraries = await db.ExecuteAsync("FUNCTION", "LIST", "LIBRARYNAME", "bookstore");
        return ((RedisResult[])libraries!).Length > 0;
    }

    public static IEndpointRouteBuilder MapInventory(this IEndpointRouteBuilder app)
    {
        app.MapPut("/inventory/{isbn}/stock", async (string isbn, StockRequest request, IConnectionMultiplexer redis) =>
        {
            await redis.GetDatabase().StringSetAsync(RedisKeys.Stock(isbn), request.Quantity);
            return Results.Ok(new { isbn, stock = request.Quantity });
        });

        app.MapPost("/inventory/{isbn}/reservations", async (string isbn, ReservationRequest request, IConnectionMultiplexer redis) =>
        {
            // FCALL reserve_stock 2 <stock> <reservations> <orderId> <quantity>
            RedisResult result = await redis.GetDatabase().ExecuteAsync("FCALL", "reserve_stock", 2,
                RedisKeys.Stock(isbn), RedisKeys.StockReservations(isbn), request.OrderId, request.Quantity);
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
