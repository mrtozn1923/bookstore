using StackExchange.Redis;

namespace Bookstore.Api;

public sealed record AddToCartRequest(string Isbn, int Quantity);

public sealed record LockPriceRequest(string Isbn, long PriceInKurus);

public sealed record CartItem(string Isbn, int Quantity, long? LockedPriceInKurus, double? PriceLockSecondsLeft);

public static class CartEndpoints
{
    private static readonly TimeSpan CartLifetime = TimeSpan.FromDays(30);
    private static readonly TimeSpan PriceLockLifetime = TimeSpan.FromMinutes(15);

    public static IEndpointRouteBuilder MapCart(this IEndpointRouteBuilder app)
    {
        // Add (or remove with a negative quantity) copies of a book: HINCRBY, then refresh the cart's lifetime
        app.MapPost("/carts/{customerId:int}/items", async (int customerId, AddToCartRequest request, IConnectionMultiplexer redis) =>
        {
            IDatabase db = redis.GetDatabase();
            RedisKey cart = RedisKeys.Cart(customerId);
            long quantity = await db.HashIncrementAsync(cart, request.Isbn, request.Quantity);
            if (quantity <= 0)
            {
                await db.HashDeleteAsync(cart, request.Isbn);
            }
            await db.KeyExpireAsync(cart, CartLifetime); // sliding: every change gives the cart 30 more days
            return Results.Ok(new { request.Isbn, quantity = Math.Max(quantity, 0) });
        });

        app.MapDelete("/carts/{customerId:int}/items/{isbn}", async (int customerId, string isbn, IConnectionMultiplexer redis) =>
        {
            bool removed = await redis.GetDatabase().HashDeleteAsync(RedisKeys.Cart(customerId), isbn);
            return removed ? Results.NoContent() : Results.NotFound();
        });

        // Campaign price is held for 15 minutes per book: HSETEX with a per-field TTL
        app.MapPost("/carts/{customerId:int}/price-locks", async (int customerId, LockPriceRequest request, IConnectionMultiplexer redis) =>
        {
            await redis.GetDatabase().HashFieldSetAndSetExpiryAsync(
                RedisKeys.CartPriceLocks(customerId), request.Isbn, request.PriceInKurus, PriceLockLifetime);
            return Results.Ok(new { request.Isbn, expiresInSeconds = (int)PriceLockLifetime.TotalSeconds });
        });

        app.MapGet("/carts/{customerId:int}", async (int customerId, IConnectionMultiplexer redis) =>
        {
            IDatabase db = redis.GetDatabase();
            RedisKey cart = RedisKeys.Cart(customerId);
            RedisKey locks = RedisKeys.CartPriceLocks(customerId);

            Task<HashEntry[]> itemsTask = db.HashGetAllAsync(cart);
            Task<HashEntry[]> locksTask = db.HashGetAllAsync(locks); // expired fields are never returned
            Task<TimeSpan?> ttlTask = db.KeyTimeToLiveAsync(cart);
            await Task.WhenAll(itemsTask, locksTask, ttlTask);

            Dictionary<string, long> lockedPrices = locksTask.Result.ToDictionary(e => (string)e.Name!, e => (long)e.Value);
            RedisValue[] lockedIsbns = [.. lockedPrices.Keys.Select(k => (RedisValue)k)];
            long[] lockTtls = lockedIsbns.Length == 0 ? [] : await db.HashFieldGetTimeToLiveAsync(locks, lockedIsbns); // HPTTL, milliseconds
            Dictionary<string, double> secondsLeft = lockedIsbns.Zip(lockTtls).ToDictionary(p => (string)p.First!, p => p.Second / 1000.0);

            CartItem[] items = [.. itemsTask.Result.Select(e => new CartItem(
                Isbn: e.Name!,
                Quantity: (int)e.Value,
                LockedPriceInKurus: lockedPrices.TryGetValue(e.Name!, out long price) ? price : null,
                PriceLockSecondsLeft: secondsLeft.TryGetValue(e.Name!, out double left) ? Math.Round(left) : null))];

            return Results.Ok(new { customerId, items, expiresInDays = ttlTask.Result?.TotalDays is double d ? Math.Round(d, 2) : (double?)null });
        });

        return app;
    }
}
