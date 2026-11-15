using Algorithms.Caching;
using BitFaster.Caching.Lfu;
using BitFaster.Caching.Lru;
using Microsoft.Extensions.Caching.Memory;

namespace Algorithms.Demos.Demos;

public static class CacheDemos
{
    public static void Basics()
    {
        LruCache<string, string> covers = new(3);
        covers.Evicted += (isbn, _) => Console.WriteLine($"evicted: {isbn}");
        covers.Put("Dune", "dune.webp");
        covers.Put("Emma", "emma.webp");
        covers.Put("The Hobbit", "hobbit.webp");
        Console.WriteLine($"order: {string.Join(", ", covers.KeysMostRecentFirst)}");
        covers.TryGet("Dune", out _);
        Console.WriteLine($"after get Dune: {string.Join(", ", covers.KeysMostRecentFirst)}");
        covers.Put("Middlemarch", "middlemarch.webp");
        Console.WriteLine($"after put Middlemarch: {string.Join(", ", covers.KeysMostRecentFirst)}");
    }

    // Half of the traffic: 80 popular covers. Other half: a sitemap crawler reading all 5,000 covers in order
    public static void ScanWorkload()
    {
        List<int> trace = [];
        Random random = new(42);
        int next = 0;
        for (int i = 0; i < 200_000; i++)
        {
            trace.Add(random.Next(2) == 0 ? random.Next(80) : 1_000 + next++ % 5_000);
        }

        const int capacity = 100;
        LruCache<int, int> lru = new(capacity);
        LfuCache<int, int> lfu = new(capacity);
        ConcurrentLru<int, int> bfLru = new(capacity);
        ConcurrentLfu<int, int> bfLfu = new(capacity);
        Console.WriteLine($"trace: {trace.Count:N0} requests, capacity {capacity}");
        Report("LruCache (ours)", trace, k => Hit(lru.TryGet, lru.Put, k));
        Report("LfuCache (ours)", trace, k => Hit(lfu.TryGet, lfu.Put, k));
        Report("BitFaster ConcurrentLru", trace, k => bfLru.TryGet(k, out _) || Add(() => bfLru.AddOrUpdate(k, k)));
        Report("BitFaster ConcurrentLfu", trace, k => bfLfu.TryGet(k, out _) || Add(() => bfLfu.AddOrUpdate(k, k)));
    }

    private delegate bool TryGetFn(int key, out int value);


    private static bool Hit(TryGetFn get, Action<int, int> put, int key)
    {
        if (get(key, out _)) return true;
        put(key, key);
        return false;
    }

    private static bool Add(Action add)
    {
        add();
        return false;
    }

    private static void Report(string name, List<int> trace, Func<int, bool> request)
    {
        int hotHits = 0, hotTotal = 0;
        foreach (int key in trace)
        {
            bool hit = request(key);
            if (key < 80)
            {
                hotTotal++;
                if (hit) hotHits++;
            }
        }

        Console.WriteLine($"{name,-24} hit ratio on popular covers: {100.0 * hotHits / hotTotal:F1}%");
    }

    public static void Threads()
    {
        LruCache<int, int> unsafeCache = new(100);
        try
        {
            Parallel.For(0, 1_000_000, i =>
            {
                int key = i % 300;
                if (!unsafeCache.TryGet(key, out _)) unsafeCache.Put(key, i);
            });
            Console.WriteLine($"no exception, Count = {unsafeCache.Count}");
        }
        catch (AggregateException ex)
        {
            Exception first = ex.InnerExceptions[0];
            Console.WriteLine($"{first.GetType().Name}: {first.Message}");
        }

        SynchronizedLruCache<int, int> safeCache = new(100);
        Parallel.For(0, 1_000_000, i =>
        {
            int key = i % 300;
            if (!safeCache.TryGet(key, out _)) safeCache.Put(key, i);
        });
        Console.WriteLine($"synchronized: Count = {safeCache.Count}");
    }

    public static void MemoryCacheSizeLimit()
    {
        MemoryCacheOptions options = new() { SizeLimit = 100 };
        Console.WriteLine($"CompactionPercentage default: {options.CompactionPercentage}");
        using MemoryCache cache = new(options);
        for (int i = 0; i < 100; i++)
        {
            cache.Set(i, $"cover-{i}", new MemoryCacheEntryOptions { Size = 1 });
            Thread.Sleep(1); // distinct LastAccessed values
        }

        for (int i = 0; i < 50; i++) cache.TryGetValue(i, out _); // 0..49 are now the most recently used

        cache.Set("new", "cover-new", new MemoryCacheEntryOptions { Size = 1 });
        Console.WriteLine($"right after Set(new): Count={cache.Count}, contains new={cache.TryGetValue("new", out _)}");
        Thread.Sleep(500); // compaction runs on the thread pool
        int[] missing = [.. Enumerable.Range(0, 100).Where(i => !cache.TryGetValue(i, out _))];
        Console.WriteLine($"after compaction: Count={cache.Count}, removed=[{string.Join(", ", missing)}]");
        cache.Set("new", "cover-new", new MemoryCacheEntryOptions { Size = 1 });
        Console.WriteLine($"Set(new) again: contains new={cache.TryGetValue("new", out _)}");
    }

    public static void Thumbnails()
    {
        Random random = new(7);
        List<(string Isbn, byte[] Data)> covers = [.. Enumerable.Range(0, 1_000)
            .Select(i => ($"978{i:D10}", new byte[random.Next(8 * 1024, 60 * 1024)]))];

        LruCache<string, byte[]> byCount = new(100);
        ByteSizedLruCache<string> byBytes = new(2 * 1024 * 1024);
        foreach ((string isbn, byte[] data) in covers)
        {
            byCount.Put(isbn, data);
            byBytes.Put(isbn, data);
        }

        long countBytes = covers.TakeLast(100).Sum(c => (long)c.Data.Length);
        Console.WriteLine($"capacity 100 entries: {byCount.Count} covers, {countBytes / 1024.0 / 1024:F2} MB");
        Console.WriteLine($"capacity 2 MB:        {byBytes.Count} covers, {byBytes.CurrentBytes / 1024.0 / 1024:F2} MB");
    }
}
