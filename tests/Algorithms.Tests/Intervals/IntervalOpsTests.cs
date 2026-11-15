using Algorithms.Intervals;

namespace Algorithms.Tests.Intervals;

public class IntervalOpsTests
{
    private static Interval<int> I(int s, int e) => new(s, e);

    [Fact]
    public void Merge_OverlappingAndTouching_ReturnsDisjointSorted()
    {
        List<Interval<int>> merged = IntervalOps.Merge([I(8, 10), I(1, 3), I(2, 6), I(10, 12), I(15, 18)]);

        Assert.Equal([I(1, 6), I(8, 12), I(15, 18)], merged);
    }

    [Fact]
    public void Merge_TouchingNotMerged_KeepsThemApart()
    {
        Assert.Equal([I(8, 10), I(10, 12)], IntervalOps.Merge([I(10, 12), I(8, 10)], mergeTouching: false));
    }

    [Fact]
    public void Overlaps_HalfOpenNeighbours_DoNotOverlap()
    {
        Assert.False(I(9, 12).Overlaps(I(12, 15)));
        Assert.True(I(9, 12).Overlaps(I(11, 15)));
    }

    [Fact]
    public void FindOverlap_LongIntervalCoversLaterOne_FindsIt()
    {
        (Interval<int> First, Interval<int> Second)? hit = IntervalOps.FindOverlap([I(1, 10), I(2, 3), I(4, 5)]);

        Assert.Equal((I(1, 10), I(2, 3)), hit);
        Assert.Null(IntervalOps.FindOverlap([I(1, 2), I(2, 3)]));
    }

    [Fact]
    public void MaxConcurrent_EndAndStartAtSameTime_AreNotCountedTogether()
    {
        Assert.Equal((1, 9), IntervalOps.MaxConcurrent([I(9, 10), I(10, 11)]));
        Assert.Equal((3, 11), IntervalOps.MaxConcurrent([I(9, 12), I(10, 13), I(11, 14), I(13, 15)]));
    }

    [Fact]
    public void Gaps_ShiftsInsideDay_ReturnsUncoveredRanges()
    {
        List<Interval<int>> gaps = IntervalOps.Gaps(I(0, 24), [I(6, 14), I(13, 20), I(22, 26)]);

        Assert.Equal([I(0, 6), I(20, 22)], gaps);
    }

    [Fact]
    public void Minimums_RandomPrices_MatchNaive()
    {
        Random random = new(25);
        decimal[] prices = [.. Enumerable.Range(0, 500).Select(_ => (decimal)random.Next(50, 200))];
        foreach (int window in new[] { 1, 7, 30, 600 })
        {
            Assert.Equal(SlidingWindow.MinimumsNaive(prices, window), SlidingWindow.Minimums(prices, window));
        }
    }
}
