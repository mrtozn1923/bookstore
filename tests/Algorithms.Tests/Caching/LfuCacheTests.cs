using Algorithms.Caching;

namespace Algorithms.Tests.Caching;

public class LfuCacheTests
{
    [Fact]
    public void Put_Full_EvictsLeastFrequentlyUsed()
    {
        LfuCache<string, int> cache = new(2);
        cache.Put("Dune", 1);
        cache.Put("Emma", 2);
        cache.TryGet("Dune", out _);
        cache.TryGet("Dune", out _);
        cache.TryGet("Emma", out _);
        cache.Put("Middlemarch", 3);

        Assert.True(cache.TryGet("Dune", out _));
        Assert.False(cache.TryGet("Emma", out _));
    }

    [Fact]
    public void Put_FullWithFrequencyTie_EvictsLeastRecentAmongThem()
    {
        LfuCache<string, int> cache = new(2);
        cache.Put("Dune", 1);
        cache.Put("Emma", 2);
        cache.Put("Middlemarch", 3);

        Assert.False(cache.TryGet("Dune", out _));
        Assert.True(cache.TryGet("Emma", out _));
    }

    [Fact]
    public void TryGet_Hit_IncrementsFrequency()
    {
        LfuCache<string, int> cache = new(2);
        cache.Put("Dune", 1);
        cache.TryGet("Dune", out _);
        cache.Put("Dune", 5);

        Assert.Equal(3, cache.FrequencyOf("Dune"));
    }
}
