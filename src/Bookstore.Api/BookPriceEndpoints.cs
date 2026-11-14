using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace Bookstore.Api;

public sealed record PriceRequest(long PriceInKurus);

public static class BookPriceEndpoints
{
    private static readonly TimeSpan LocalLifetime = TimeSpan.FromMinutes(10);

    public static string CacheKey(string isbn) => $"price:{isbn}";

    public static IEndpointRouteBuilder MapBookPrices(this IEndpointRouteBuilder app)
    {
        // L1 (this instance's memory) first, then Redis
        app.MapGet("/books/{isbn}/price", async (string isbn, IMemoryCache local, IConnectionMultiplexer redis, IConfiguration config) =>
        {
            string instance = config["InstanceName"] ?? "api";
            if (local.TryGetValue(CacheKey(isbn), out long cached))
            {
                return Results.Ok(new { isbn, priceInKurus = cached, source = "l1", instance });
            }
            RedisValue price = await redis.GetDatabase().HashGetAsync(RedisKeys.Prices, isbn);
            if (price.IsNull)
            {
                return Results.NotFound();
            }
            local.Set(CacheKey(isbn), (long)price, LocalLifetime);
            return Results.Ok(new { isbn, priceInKurus = (long)price, source = "redis", instance });
        });

        // Write the new price, then tell every instance to drop its L1 copy
        app.MapPut("/books/{isbn}/price", async (string isbn, PriceRequest request, IConnectionMultiplexer redis) =>
        {
            IDatabase db = redis.GetDatabase();
            await db.HashSetAsync(RedisKeys.Prices, isbn, request.PriceInKurus);
            long receivers = await db.PublishAsync(RedisKeys.PriceChanged, isbn);            // PUBLISH
            return Results.Ok(new { isbn, request.PriceInKurus, notifiedInstances = receivers });
        });

        return app;
    }
}
