using StackExchange.Redis;

namespace Bookstore.Api;

public static class TagEndpoints
{
    public static IEndpointRouteBuilder MapTags(this IEndpointRouteBuilder app)
    {
        app.MapPost("/books/{isbn}/tags/{tag}", async (string isbn, string tag, IConnectionMultiplexer redis) =>
        {
            bool added = await redis.GetDatabase().SetAddAsync(RedisKeys.Tag(tag), isbn);
            return Results.Ok(new { isbn, tag, added });
        });

        // Books that carry every given tag: SINTER; the count alone: SINTERCARD
        app.MapGet("/books/by-tags", async (string[] tag, IConnectionMultiplexer redis) =>
        {
            IDatabase db = redis.GetDatabase();
            RedisKey[] keys = [.. tag.Select(RedisKeys.Tag)];
            Task<RedisValue[]> books = db.SetCombineAsync(SetOperation.Intersect, keys);
            Task<long> count = db.SetIntersectionLengthAsync(keys);
            await Task.WhenAll(books, count);
            return Results.Ok(new { tags = tag, count = count.Result, isbns = books.Result.Select(b => b.ToString()) });
        });

        // A few random books from a tag for the "you may also like" box: SRANDMEMBER
        app.MapGet("/books/suggestions", async (string tag, int count, IConnectionMultiplexer redis) =>
        {
            RedisValue[] isbns = await redis.GetDatabase().SetRandomMembersAsync(RedisKeys.Tag(tag), count);
            return Results.Ok(isbns.Select(i => i.ToString()));
        });

        return app;
    }
}
