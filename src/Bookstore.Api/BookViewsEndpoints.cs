using StackExchange.Redis;

namespace Bookstore.Api;

public static class BookViewsEndpoints
{
    public static IEndpointRouteBuilder MapBookViews(this IEndpointRouteBuilder app)
    {
        app.MapPost("/books/{bookId:int}/views", async (int bookId, IConnectionMultiplexer redis) =>
        {
            long views = await redis.GetDatabase().StringIncrementAsync(RedisKeys.BookViews(bookId));
            return Results.Ok(new { bookId, views });
        });

        app.MapGet("/books/{bookId:int}/views", async (int bookId, IConnectionMultiplexer redis) =>
        {
            RedisValue views = await redis.GetDatabase().StringGetAsync(RedisKeys.BookViews(bookId));
            return Results.Ok(new { bookId, views = views.IsNull ? 0 : (long)views });
        });

        return app;
    }
}
