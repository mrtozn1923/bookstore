using System.Diagnostics;
using StackExchange.Redis;

namespace Bookstore.RedisLab;

public static class SortedSetDemo
{
    private static readonly DateOnly Today = new(2026, 11, 14);

    private static RedisKey Daily(DateOnly day) => $"bookstore:bestsellers:{day:yyyyMMdd}";

    // Seven daily bestseller sets, combined three ways: SUM, COUNT and recency weights
    public static async Task BestsellersAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=observer,allowAdmin=true");
        IDatabase db = mux.GetDatabase();
        await mux.GetServers()[0].FlushDatabaseAsync();

        // 2,000 books; a few steady sellers, one book that sold 300 copies on a single day
        Random random = new(7);
        RedisKey[] days = [.. Enumerable.Range(0, 7).Select(i => Daily(Today.AddDays(i - 6)))];
        for (int d = 0; d < days.Length; d++)
        {
            SortedSetEntry[] sales = [.. Enumerable.Range(1, 2_000)
                .Select(book => new SortedSetEntry($"978000000{book:D4}", random.Next(0, book <= 20 ? 40 : 12)))
                .Where(e => e.Score > 0)];
            await db.SortedSetAddAsync(days[d], sales);
        }
        await db.SortedSetIncrementAsync(days[0], "9780441013593", 300); // Dune: one campaign day, six days ago

        RedisKey week = "bookstore:bestsellers:week:20261114";
        RedisKey weekDays = "bookstore:bestsellers:week:20261114:days";
        RedisKey trending = "bookstore:bestsellers:trending:20261114";
        await db.SortedSetCombineAndStoreAsync(SetOperation.Union, week, days);                          // ZUNIONSTORE ... (SUM)
        await db.SortedSetCombineAndStoreAsync(SetOperation.Union, weekDays, days, null, Aggregate.Count); // AGGREGATE COUNT
        double[] weights = [.. Enumerable.Range(1, 7).Select(i => i / 7.0)];                               // yesterday counts more
        await db.SortedSetCombineAndStoreAsync(SetOperation.Union, trending, days, weights);

        await PrintTopAsync(db, "SUM      (copies in 7 days)", week, weekDays);
        await PrintTopAsync(db, "weighted (recent days count more)", trending, weekDays);
        long everyDay = await db.SortedSetLengthAsync(weekDays, 7, 7);                                   // ZCOUNT key 7 7
        Console.WriteLine($"books sold on all 7 days: {everyDay:N0} of {await db.SortedSetLengthAsync(weekDays):N0}");
    }

    private static async Task PrintTopAsync(IDatabase db, string title, RedisKey key, RedisKey daysKey)
    {
        SortedSetEntry[] top = await db.SortedSetRangeByRankWithScoresAsync(key, 0, 4, Order.Descending);
        double?[] daysSold = await db.SortedSetScoresAsync(daysKey, [.. top.Select(e => e.Element)]);    // ZMSCORE
        Console.WriteLine(title);
        for (int i = 0; i < top.Length; i++)
        {
            Console.WriteLine($"  {i + 1}. {top[i].Element}  score={top[i].Score,7:F1}  days={daysSold[i]}");
        }
    }

    // Sales of the six days before today (UTC), for the /bestsellers/week endpoint
    public static async Task SeedWeekAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=observer");
        IDatabase db = mux.GetDatabase();
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        string[][] sales =
        [
            ["4", "9780441013593", "9", "9780451524935", "2", "9780141439518"],
            ["5", "9780441013593", "3", "9780451524935", "6", "9780547928227"],
            ["3", "9780441013593", "8", "9780451524935"],
            ["6", "9780441013593", "1", "9780141439549", "7", "9780547928227"],
            ["2", "9780441013593", "4", "9780451524935", "2", "9780553293357"],
            ["4", "9780441013593", "2", "9780451524935", "3", "9780060850524"],
        ];
        for (int i = 0; i < sales.Length; i++)
        {
            RedisKey key = Daily(today.AddDays(i - 6));
            SortedSetEntry[] entries = [.. sales[i].Chunk(2).Select(p => new SortedSetEntry(p[1], double.Parse(p[0])))];
            await db.SortedSetAddAsync(key, entries);
            await db.KeyExpireAsync(key, TimeSpan.FromDays(8), ExpireWhen.HasNoExpiry);
            Console.WriteLine($"{key}: {entries.Length} books");
        }
    }

    // Encoding and memory: 128 versus 129 members, then one million members
    public static async Task EncodingAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=observer,allowAdmin=true");
        IDatabase db = mux.GetDatabase();
        IServer server = mux.GetServers()[0];
        await server.FlushDatabaseAsync();

        foreach (int size in new[] { 128, 129 })
        {
            RedisKey key = $"bookstore:bestsellers:size-{size}";
            await db.SortedSetAddAsync(key, [.. Enumerable.Range(1, size).Select(i => new SortedSetEntry($"978000000{i:D4}", i))]);
            Console.WriteLine($"{size,9:N0} members: {await MemoryAsync(db, key),12:N0} bytes, encoding={await db.ExecuteAsync("OBJECT", "ENCODING", key)}");
        }

        RedisKey big = "bookstore:bestsellers:all-time";
        for (int start = 0; start < 1_000_000; start += 10_000)
        {
            await db.SortedSetAddAsync(big, [.. Enumerable.Range(start, 10_000)
                .Select(i => new SortedSetEntry($"978{i:D10}", i % 5_000))]);
        }
        Console.WriteLine($"{1_000_000,9:N0} members: {await MemoryAsync(db, big),12:N0} bytes, encoding={await db.ExecuteAsync("OBJECT", "ENCODING", big)}");

        await server.ConfigResetStatisticsAsync();
        await server.SlowlogResetAsync();
        for (int i = 0; i < 10_000; i++)
        {
            await db.SortedSetRankAsync(big, $"978{i * 97:D10}", Order.Descending);                      // ZREVRANK
        }
        SortedSetEntry[] page = await db.SortedSetRangeByRankWithScoresAsync(big, 500_000, 500_049, Order.Descending);
        Stopwatch sw = Stopwatch.StartNew();
        RedisValue[] all = await db.SortedSetRangeByRankAsync(big, 0, -1);                                // ZRANGE key 0 -1
        Console.WriteLine($"ZRANGE 0 -1: {all.Length:N0} members in {sw.ElapsedMilliseconds:N0} ms (client side), page at rank 500,000: {page.Length} entries");

        foreach (string line in (await server.InfoAsync("commandstats")).SelectMany(g => g)
                     .Where(kv => kv.Key is "cmdstat_zrevrank" or "cmdstat_zrange" or "cmdstat_zrevrange").Select(kv => $"{kv.Key}: {kv.Value}"))
        {
            Console.WriteLine(line);
        }
        foreach (CommandTrace trace in await server.SlowlogGetAsync(10))
        {
            Console.WriteLine($"slowlog: {string.Join(' ', trace.Arguments.Take(4))} {trace.Duration.TotalMilliseconds:F1} ms");
        }
    }

    private static async Task<long> MemoryAsync(IDatabase db, RedisKey key) => (long)await db.ExecuteAsync("MEMORY", "USAGE", key);

    // Scores are IEEE 754 doubles: integers are exact only up to 2^53
    public static async Task ScorePrecisionAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=bookstore-api");
        IDatabase db = mux.GetDatabase();
        RedisKey key = "bookstore:bestsellers:composite";
        await db.KeyDeleteAsync(key);

        // Composite score: copies sold * 10^13 + first-sale time in Unix milliseconds
        long firstSaleA = 1_763_000_000_000, firstSaleB = 1_763_000_000_001;
        foreach (long sold in new long[] { 12, 900, 901 })
        {
            long a = sold * 10_000_000_000_000 + firstSaleA;
            long b = sold * 10_000_000_000_000 + firstSaleB;
            await db.SortedSetAddAsync(key, [new SortedSetEntry("book-a", a), new SortedSetEntry("book-b", b)]);
            double? storedA = await db.SortedSetScoreAsync(key, "book-a");
            double? storedB = await db.SortedSetScoreAsync(key, "book-b");
            Console.WriteLine($"sold={sold,3}  sent a={a} b={b}  stored a={storedA:F0} b={storedB:F0}  equal={storedA == storedB}");
        }
        Console.WriteLine($"2^53 = {1L << 53}");
    }

    // Paging a live leaderboard by rank: a sale between two page requests
    public static async Task PageDriftAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=bookstore-api");
        IDatabase db = mux.GetDatabase();
        RedisKey live = Daily(Today);
        RedisKey snapshot = "bookstore:bestsellers:20261114:snapshot";
        await db.KeyDeleteAsync([live, snapshot]);
        await db.SortedSetAddAsync(live, [.. Enumerable.Range(1, 6).Select(i => new SortedSetEntry($"book-{i}", 70 - i * 10))]);

        foreach (bool useSnapshot in new[] { false, true })
        {
            RedisKey source = live;
            if (useSnapshot)
            {
                await db.SortedSetRangeAndStoreAsync(live, snapshot, 0, -1);                              // ZRANGESTORE
                await db.KeyExpireAsync(snapshot, TimeSpan.FromMinutes(5));
                source = snapshot;
            }
            RedisValue[] page1 = await db.SortedSetRangeByRankAsync(source, 0, 2, Order.Descending);
            await db.SortedSetIncrementAsync(live, "book-5", 50);                                         // a sale between the two requests
            RedisValue[] page2 = await db.SortedSetRangeByRankAsync(source, 3, 5, Order.Descending);
            Console.WriteLine($"{(useSnapshot ? "snapshot" : "live"),-8}  page 1: {string.Join(", ", page1)}  page 2: {string.Join(", ", page2)}");
            await db.SortedSetIncrementAsync(live, "book-5", -50);
        }
    }

    // Orders in the last five minutes: the timestamp is the score
    public static async Task RecentOrdersAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=bookstore-api");
        IDatabase db = mux.GetDatabase();
        RedisKey key = "bookstore:orders:recent";
        await db.KeyDeleteAsync(key);

        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        // 600 orders spread over the last 10 minutes, one per second
        await db.SortedSetAddAsync(key, [.. Enumerable.Range(0, 600).Select(i => new SortedSetEntry($"order-{5000 + i}", now - (600 - i) * 1000L))]);

        long windowStart = now - (long)TimeSpan.FromMinutes(5).TotalMilliseconds;
        long inWindow = await db.SortedSetLengthAsync(key, windowStart, double.PositiveInfinity);        // ZCOUNT key start +inf
        long removed = await db.SortedSetRemoveRangeByScoreAsync(key, double.NegativeInfinity, windowStart, Exclude.Stop); // ZREMRANGEBYSCORE key -inf (start
        Console.WriteLine($"orders in the last 5 minutes: {inWindow}, removed older entries: {removed}, left: {await db.SortedSetLengthAsync(key)}");
        SortedSetEntry[] last = await db.SortedSetRangeByRankWithScoresAsync(key, 0, 0, Order.Descending);
        Console.WriteLine($"newest: {last[0].Element}, {now - (long)last[0].Score} ms ago");
    }
}
