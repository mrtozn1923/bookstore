using Algorithms.Probabilistic;

namespace Algorithms.Tests.Probabilistic;

public class BloomFilterTests
{
    [Fact]
    public void MightContain_AddedItems_IsAlwaysTrue()
    {
        BloomFilter filter = BloomFilter.ForCapacity(10_000, 0.01);
        for (int i = 0; i < 10_000; i++) filter.Add($"user{i}@example.com");

        Assert.All(Enumerable.Range(0, 10_000), i => Assert.True(filter.MightContain($"user{i}@example.com")));
    }

    [Fact]
    public void MightContain_NewItems_FalsePositiveRateNearTarget()
    {
        BloomFilter filter = BloomFilter.ForCapacity(10_000, 0.01);
        for (int i = 0; i < 10_000; i++) filter.Add($"user{i}@example.com");
        int falsePositives = Enumerable.Range(0, 100_000).Count(i => filter.MightContain($"new{i}@example.com"));

        Assert.InRange(falsePositives / 100_000.0, 0.005, 0.02);
    }

    [Fact]
    public void ForCapacity_OnePercent_UsesAboutTenBitsAndSevenHashes()
    {
        BloomFilter filter = BloomFilter.ForCapacity(1_000_000, 0.01);

        Assert.Equal(7, filter.HashCount);
        Assert.InRange(filter.BitCount / 1_000_000.0, 9.5, 9.7);
    }
}

public class CountMinSketchTests
{
    [Fact]
    public void Estimate_NeverBelowTrueCount()
    {
        CountMinSketch sketch = CountMinSketch.ForError(0.01, 0.01);
        Random random = new(26);
        Dictionary<string, int> exact = [];
        for (int i = 0; i < 50_000; i++)
        {
            string isbn = $"978{random.Next(5_000):D10}";
            sketch.Add(isbn);
            exact[isbn] = exact.GetValueOrDefault(isbn) + 1;
        }

        Assert.All(exact, kv => Assert.True(sketch.Estimate(kv.Key) >= kv.Value));
        Assert.All(exact, kv => Assert.True(sketch.Estimate(kv.Key) - kv.Value <= 0.01 * 50_000 * 2));
    }

    [Fact]
    public void ForError_ComputesWidthAndDepth()
    {
        CountMinSketch sketch = CountMinSketch.ForError(0.001, 0.01);

        Assert.Equal(2719, sketch.Width);
        Assert.Equal(5, sketch.Depth);
    }
}

public class HyperLogLogTests
{
    [Theory]
    [InlineData(100)]
    [InlineData(10_000)]
    [InlineData(1_000_000)]
    public void Count_DistinctItems_WithinThreeStandardErrors(int distinct)
    {
        HyperLogLog hll = new(14);
        for (int round = 0; round < 2; round++)
            for (int i = 0; i < distinct; i++) hll.Add($"visitor-{i}");

        double error = Math.Abs(hll.Count() - distinct) / (double)distinct;
        Assert.True(error < 3 * hll.StandardError, $"error {error:P2}");
    }

    [Fact]
    public void Merge_TwoDays_CountsUnion()
    {
        HyperLogLog monday = new(14), tuesday = new(14);
        for (int i = 0; i < 60_000; i++) monday.Add($"visitor-{i}");
        for (int i = 40_000; i < 100_000; i++) tuesday.Add($"visitor-{i}");
        monday.Merge(tuesday);

        Assert.InRange(monday.Count(), 97_000, 103_000);
    }
}
