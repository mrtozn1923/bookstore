using Algorithms.Caching;

namespace Algorithms.Tests.Caching;

public class ByteSizedLruCacheTests
{
    [Fact]
    public void Put_OverByteLimit_EvictsUntilItFits()
    {
        ByteSizedLruCache<string> cache = new(100);
        cache.Put("a", new byte[40]);
        cache.Put("b", new byte[40]);
        cache.Put("c", new byte[50]);

        Assert.False(cache.TryGet("a", out _));
        Assert.True(cache.TryGet("b", out _));
        Assert.Equal(90, cache.CurrentBytes);
    }

    [Fact]
    public void Put_ItemLargerThanLimit_ReturnsFalseAndKeepsCache()
    {
        ByteSizedLruCache<string> cache = new(100);
        cache.Put("a", new byte[40]);

        Assert.False(cache.Put("huge", new byte[101]));
        Assert.Equal(1, cache.Count);
    }
}
