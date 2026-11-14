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

    // {isbn} is a hash tag: a book's stock and reservations always land in the same cluster slot
    public static RedisKey Stock(string isbn) => $"{Prefix}:stock:{{{isbn}}}";

    public static RedisKey StockReservations(string isbn) => $"{Prefix}:stock:{{{isbn}}}:reservations";

    public static RedisKey VerificationRequests(int customerId) => $"{Prefix}:verify:{customerId}:requests";

    // Order events for the workers (Streams, post 10)
    public static RedisKey OrderEvents => $"{Prefix}:orders:events";

    // Book prices in kurus: one hash, field = ISBN
    public static RedisKey Prices => $"{Prefix}:prices";

    // Pub/sub channel: the ISBN of a book whose price changed
    public static readonly RedisChannel PriceChanged = RedisChannel.Literal($"{Prefix}:events:price-changed");

    public static RedisKey OrderIdSequence => $"{Prefix}:order:id-seq";

    public static RedisKey Bestsellers(DateOnly day) => $"{Prefix}:bestsellers:{day:yyyyMMdd}";

    public static RedisKey BestsellersWeek(DateOnly lastDay) => $"{Prefix}:bestsellers:week:{lastDay:yyyyMMdd}";

    // Temporary input of the snapshot build, e.g. bookstore:bestsellers:week:20261114:sold
    public static RedisKey BestsellersWeekPart(DateOnly lastDay, string part) =>
        $"{Prefix}:bestsellers:week:{lastDay:yyyyMMdd}:{part}";

    public static RedisKey RecentOrders => $"{Prefix}:orders:recent";
}
