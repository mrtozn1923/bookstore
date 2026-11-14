using System.Diagnostics;
using StackExchange.Redis;

namespace Bookstore.RedisLab;

public static class SpecialTypesDemo
{
    // The visitor data of post 05 (same seed), stored three ways: set, bitmap, HyperLogLog
    public static async Task UniqueVisitorsAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=observer,allowAdmin=true");
        IDatabase db = mux.GetDatabase();
        await mux.GetServers()[0].FlushDatabaseAsync();

        Random random = new(42);
        string[] days = [.. Enumerable.Range(8, 7).Select(d => $"202611{d:D2}")];
        foreach (string day in days)
        {
            int[] customerIds = [.. Enumerable.Range(0, 20_000).Select(_ => random.Next(1, 50_001))];
            await db.SetAddAsync($"bookstore:visitors:{day}", [.. customerIds.Select(id => (RedisValue)id)]);  // SADD
            await db.HyperLogLogAddAsync($"bookstore:visitors:hll:{day}", [.. customerIds.Select(id => (RedisValue)id)]); // PFADD
            IBatch batch = db.CreateBatch();
            Task[] bits = [.. customerIds.Select(id => batch.StringSetBitAsync($"bookstore:active:{day}", id, true))]; // SETBIT
            batch.Execute();
            await Task.WhenAll(bits);
        }

        string first = days[0];
        Console.WriteLine($"one day ({first}):");
        Console.WriteLine($"  set    {await db.SetLengthAsync($"bookstore:visitors:{first}"),7:N0} visitors  {await MemoryAsync(db, $"bookstore:visitors:{first}"),9:N0} bytes");
        Console.WriteLine($"  bitmap {await db.StringBitCountAsync($"bookstore:active:{first}"),7:N0} visitors  {await MemoryAsync(db, $"bookstore:active:{first}"),9:N0} bytes");
        Console.WriteLine($"  hll    {await db.HyperLogLogLengthAsync($"bookstore:visitors:hll:{first}"),7:N0} visitors  {await MemoryAsync(db, $"bookstore:visitors:hll:{first}"),9:N0} bytes");

        RedisKey[] sets = [.. days.Select(d => (RedisKey)$"bookstore:visitors:{d}")];
        RedisKey[] bitmaps = [.. days.Select(d => (RedisKey)$"bookstore:active:{d}")];
        RedisKey[] hlls = [.. days.Select(d => (RedisKey)$"bookstore:visitors:hll:{d}")];

        long exact = await db.SetCombineLengthAsync(SetOperation.Union, sets);                          // SUNIONCARD
        await db.StringBitOperationAsync(Bitwise.Or, "bookstore:active:week", bitmaps);                  // BITOP OR
        await db.StringBitOperationAsync(Bitwise.And, "bookstore:active:every-day", bitmaps);            // BITOP AND
        long estimated = await db.HyperLogLogLengthAsync(hlls);                                           // PFCOUNT k1 ... k7
        Console.WriteLine("7 days:");
        Console.WriteLine($"  SUNIONCARD          {exact,7:N0}");
        Console.WriteLine($"  BITOP OR + BITCOUNT {await db.StringBitCountAsync("bookstore:active:week"),7:N0}");
        Console.WriteLine($"  PFCOUNT (7 keys)    {estimated,7:N0}  error {Math.Abs(estimated - exact) * 100.0 / exact:F2} %");
        Console.WriteLine($"  every day: BITOP AND + BITCOUNT {await db.StringBitCountAsync("bookstore:active:every-day"):N0}, " +
                          $"SINTERCARD {await db.SetIntersectionLengthAsync(sets):N0}");
    }

    // One million distinct customers: memory and error of each structure
    public static async Task UniqueScaleAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=observer,allowAdmin=true");
        IDatabase db = mux.GetDatabase();
        await mux.GetServers()[0].FlushDatabaseAsync();

        foreach (int count in new[] { 100, 10_000, 1_000_000 })
        {
            RedisKey set = $"scale:set:{count}", hll = $"scale:hll:{count}", bitmap = $"scale:bitmap:{count}";
            for (int start = 1; start <= count; start += 10_000)
            {
                RedisValue[] ids = [.. Enumerable.Range(start, Math.Min(10_000, count - start + 1)).Select(i => (RedisValue)i)];
                await db.SetAddAsync(set, ids);
                await db.HyperLogLogAddAsync(hll, ids);
            }
            await db.StringSetBitAsync(bitmap, count, true); // only the size matters here: the highest id decides it
            long estimate = await db.HyperLogLogLengthAsync(hll);
            Console.WriteLine($"{count,9:N0} customers  set {await MemoryAsync(db, set),12:N0} B   bitmap {await MemoryAsync(db, bitmap),9:N0} B   " +
                              $"hll {await MemoryAsync(db, hll),6:N0} B  estimate {estimate,9:N0} ({(estimate - count) * 100.0 / count:+0.00;-0.00} %)");
        }
    }

    // Bloom filter, Top-K, Count-Min Sketch and t-digest against exact answers
    public static async Task ProbabilisticAsync(string connectionString)
    {
        await using ConnectionMultiplexer mux = await ConnectionMultiplexer.ConnectAsync(connectionString + ",name=observer,allowAdmin=true");
        IDatabase db = mux.GetDatabase();
        await mux.GetServers()[0].FlushDatabaseAsync();

        // Bloom: one million ISBNs in the catalog, 100,000 that are not
        RedisKey filter = "bookstore:catalog:isbn-filter";
        await db.ExecuteAsync("BF.RESERVE", filter, 0.01, 1_000_000);
        for (long start = 0; start < 1_000_000; start += 10_000)
        {
            object[] args = [filter, .. Enumerable.Range(0, 10_000).Select(i => (object)Isbn(start + i))];
            await db.ExecuteAsync("BF.MADD", args);
        }
        long falsePositives = 0;
        for (long start = 5_000_000; start < 5_100_000; start += 10_000)
        {
            object[] args = [filter, .. Enumerable.Range(0, 10_000).Select(i => (object)Isbn(start + i))];
            RedisResult[] found = (RedisResult[])(await db.ExecuteAsync("BF.MEXISTS", args))!;
            falsePositives += found.Count(r => (long)r == 1);
        }
        RedisKey isbnSet = "bookstore:catalog:isbns";
        for (long start = 0; start < 1_000_000; start += 10_000)
        {
            await db.SetAddAsync(isbnSet, [.. Enumerable.Range(0, 10_000).Select(i => (RedisValue)Isbn(start + i))]);
        }
        Console.WriteLine($"bloom: 1,000,000 ISBNs in {await MemoryAsync(db, filter):N0} bytes (set: {await MemoryAsync(db, isbnSet):N0} bytes), " +
                          $"false positives {falsePositives:N0} of 100,000 ({falsePositives / 1000.0:F2} %)");

        // Search terms: a few very popular ones and a long tail
        string[] popular = ["dune", "hobbit", "tolkien", "austen", "orwell", "emma", "middlemarch", "foundation"];
        Random random = new(11);
        List<string> searches = [];
        for (int i = 0; i < 100_000; i++)
        {
            double r = random.NextDouble();
            searches.Add(r < 0.6 ? popular[(int)(Math.Pow(random.NextDouble(), 2) * popular.Length)] : $"q{random.Next(1, 20_000)}");
        }
        Dictionary<string, int> exact = searches.GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count());

        await db.ExecuteAsync("TOPK.RESERVE", "bookstore:search:topk", 5);
        await db.ExecuteAsync("CMS.INITBYPROB", "bookstore:search:cms", 0.001, 0.01);
        foreach (string[] chunk in searches.Chunk(1_000))
        {
            object[] topkArgs = ["bookstore:search:topk", .. chunk];
            object[] cmsArgs = ["bookstore:search:cms", .. chunk.SelectMany(t => new object[] { t, 1 })];
            await db.ExecuteAsync("TOPK.ADD", topkArgs);
            await db.ExecuteAsync("CMS.INCRBY", cmsArgs);
        }
        RedisResult[] topk = (RedisResult[])(await db.ExecuteAsync("TOPK.LIST", "bookstore:search:topk", "WITHCOUNT"))!;
        Console.WriteLine("top-k (estimate / exact):");
        for (int i = 0; i < topk.Length; i += 2)
        {
            Console.WriteLine($"  {topk[i],-8} {(long)topk[i + 1],6:N0} / {exact[(string)topk[i]!],6:N0}");
        }
        string[] probe = ["dune", "middlemarch", "q42", "never-searched"];
        object[] queryArgs = ["bookstore:search:cms", .. probe];
        RedisResult[] counts = (RedisResult[])(await db.ExecuteAsync("CMS.QUERY", queryArgs))!;
        Console.WriteLine("count-min sketch (estimate / exact): " + string.Join(", ",
            probe.Select((t, i) => $"{t} {(long)counts[i]:N0} / {exact.GetValueOrDefault(t):N0}")));
        Console.WriteLine($"memory: top-k {await MemoryAsync(db, "bookstore:search:topk"):N0} B, cms {await MemoryAsync(db, "bookstore:search:cms"):N0} B, " +
                          $"distinct terms {exact.Count:N0}");

        // t-digest: order totals in TRY, percentiles
        double[] totals = [.. Enumerable.Range(0, 100_000).Select(_ => Math.Round(Math.Exp(5 + 0.6 * Gaussian(random)), 2))];
        await db.ExecuteAsync("TDIGEST.CREATE", "bookstore:orders:totals", "COMPRESSION", 100);
        foreach (double[] chunk in totals.Chunk(1_000))
        {
            object[] args = ["bookstore:orders:totals", .. chunk.Select(v => (object)v)];
            await db.ExecuteAsync("TDIGEST.ADD", args);
        }
        RedisResult[] q = (RedisResult[])(await db.ExecuteAsync("TDIGEST.QUANTILE", "bookstore:orders:totals", 0.5, 0.95, 0.99))!;
        double[] sorted = [.. totals.Order()];
        double Exact(double p) => sorted[(int)Math.Ceiling(p * sorted.Length) - 1];
        Console.WriteLine($"t-digest (estimate / exact): p50 {(double)q[0]:F2} / {Exact(0.5):F2}, p95 {(double)q[1]:F2} / {Exact(0.95):F2}, " +
                          $"p99 {(double)q[2]:F2} / {Exact(0.99):F2}, {await MemoryAsync(db, "bookstore:orders:totals"):N0} bytes");
    }

    private static string Isbn(long n) => $"978{n:D10}";

    private static double Gaussian(Random random) =>
        Math.Sqrt(-2 * Math.Log(1 - random.NextDouble())) * Math.Cos(2 * Math.PI * random.NextDouble());

    private static async Task<long> MemoryAsync(IDatabase db, RedisKey key) => (long)await db.ExecuteAsync("MEMORY", "USAGE", key);
}
