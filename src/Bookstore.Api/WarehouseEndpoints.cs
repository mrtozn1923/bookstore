using StackExchange.Redis;

namespace Bookstore.Api;

public static class WarehouseEndpoints
{
    public static IEndpointRouteBuilder MapWarehouses(this IEndpointRouteBuilder app)
    {
        // Nearest warehouse to the delivery address: GEOSEARCH ... BYRADIUS ... ASC COUNT 1 WITHDIST
        app.MapGet("/warehouses/nearest", async (double lon, double lat, IConnectionMultiplexer redis, double radiusKm = 400) =>
        {
            GeoRadiusResult[] found = await redis.GetDatabase().GeoSearchAsync(RedisKeys.Warehouses, lon, lat,
                new GeoSearchCircle(radiusKm, GeoUnit.Kilometers), count: 1, demandClosest: true,
                order: Order.Ascending, options: GeoRadiusOptions.WithDistance);
            return found.Length == 0
                ? Results.NotFound(new { error = $"no warehouse within {radiusKm} km" })
                : Results.Ok(new { warehouse = found[0].Member.ToString(), distanceKm = found[0].Distance });
        });

        return app;
    }
}
