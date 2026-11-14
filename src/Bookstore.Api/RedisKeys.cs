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

    public static RedisKey DailyVisitorsHll(DateOnly day) => $"{Prefix}:visitors:hll:{day:yyyyMMdd}";

    public static RedisKey ActiveCustomers(DateOnly day) => $"{Prefix}:active:{day:yyyyMMdd}";

    public static RedisKey Warehouses => $"{Prefix}:warehouses";

    public static RedisKey OrderIdSequence => $"{Prefix}:order:id-seq";

    public static RedisKey Bestsellers(DateOnly day) => $"{Prefix}:bestsellers:{day:yyyyMMdd}";

    public static RedisKey BestsellersWeek(DateOnly lastDay) => $"{Prefix}:bestsellers:week:{lastDay:yyyyMMdd}";

    // Temporary input of one snapshot build, e.g. bookstore:bestsellers:week:20261114:sold:3f2a...
    public static RedisKey BestsellersWeekPart(DateOnly lastDay, string part, string buildId) =>
        $"{Prefix}:bestsellers:week:{lastDay:yyyyMMdd}:{part}:{buildId}";

    public static RedisKey RecentOrders => $"{Prefix}:orders:recent";
}
