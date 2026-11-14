using StackExchange.Redis;

namespace Bookstore.Api;

// Bookstore key naming: bookstore:<entity>:<id>[:<detail>]
public static class RedisKeys
{
    private const string Prefix = "bookstore";

    public static RedisKey BookViews(int bookId) => $"{Prefix}:book:{bookId}:views";

    public static RedisKey BookViewsDaily(int bookId, DateOnly day) => $"{Prefix}:book:{bookId}:views:{day:yyyyMMdd}";

    public static RedisKey VerificationCode(int customerId) => $"{Prefix}:verify:{customerId}";

    public static RedisKey Idempotency(string key) => $"{Prefix}:idem:{key}";

    public static RedisKey Cart(int customerId) => $"{Prefix}:cart:{customerId}";

    public static RedisKey CartPriceLocks(int customerId) => $"{Prefix}:cart:{customerId}:price-locks";

    public static RedisKey RecentlyViewed(int customerId) => $"{Prefix}:customer:{customerId}:recent";

    public static RedisKey Tag(string tag) => $"{Prefix}:tag:{tag}";

    public static RedisKey DailyVisitors(DateOnly day) => $"{Prefix}:visitors:{day:yyyyMMdd}";

    public static RedisKey OrderIdSequence => $"{Prefix}:order:id-seq";
}
