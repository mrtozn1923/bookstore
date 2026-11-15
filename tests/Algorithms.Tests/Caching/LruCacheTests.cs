using Algorithms.Caching;

namespace Algorithms.Tests.Caching;

public class LruCacheTests
{
    [Fact]
    public void Put_BeyondCapacity_EvictsLeastRecentlyUsed()
    {
        LruCache<string, int> cache = new(2);
        cache.Put("Dune", 1);
        cache.Put("Emma", 2);
        cache.TryGet("Dune", out _);
        cache.Put("Middlemarch", 3);

        Assert.False(cache.TryGet("Emma", out _));
        Assert.Equal(["Middlemarch", "Dune"], cache.KeysMostRecentFirst);
    }

    [Fact]
    public void Put_ExistingKey_UpdatesValueWithoutEviction()
    {
        LruCache<string, int> cache = new(2);
        cache.Put("Dune", 1);
        cache.Put("Emma", 2);
        cache.Put("Dune", 10);

        Assert.Equal(2, cache.Count);
        Assert.True(cache.TryGet("Dune", out int value));
        Assert.Equal(10, value);
    }

    [Fact]
    public void Put_Full_RaisesEvictedWithOldestEntry()
    {
        LruCache<string, int> cache = new(1);
        List<string> evicted = [];
        cache.Evicted += (key, _) => evicted.Add(key);
        cache.Put("Dune", 1);
        cache.Put("Emma", 2);

        Assert.Equal(["Dune"], evicted);
    }

    [Fact]
    public void Remove_ExistingKey_RemovesNodeFromListToo()
    {
        LruCache<string, int> cache = new(3);
        cache.Put("Dune", 1);
        cache.Put("Emma", 2);
        cache.Remove("Dune");
        cache.Put("Dune", 3);

        Assert.Equal(cache.Count, cache.ListCount);
    }

    [Fact]
    public void RandomOperations_MatchNaiveReference()
    {
        Random random = new(23);
        LruCache<int, int> fast = new(16);
        NaiveLruCache<int, int> reference = new(16);
        for (int i = 0; i < 20_000; i++)
        {
            int key = random.Next(40);
            if (random.Next(2) == 0)
            {
                fast.Put(key, i);
                reference.Put(key, i);
            }
            else
            {
                bool a = fast.TryGet(key, out int va);
                bool b = reference.TryGet(key, out int vb);
                Assert.Equal(b, a);
                Assert.Equal(vb, va);
            }
        }
    }
}
