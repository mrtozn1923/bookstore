using Algorithms.Randomness;

namespace Algorithms.Tests.Randomness;

public class RandomnessTests
{
    [Fact]
    public void FisherYates_ThreeItems_AllSixPermutationsNearlyEqual()
    {
        Random random = new(27);
        Dictionary<string, int> counts = [];
        for (int t = 0; t < 60_000; t++)
        {
            char[] items = ['A', 'B', 'C'];
            Shuffles.FisherYates<char>(items, random);
            string key = new(items);
            counts[key] = counts.GetValueOrDefault(key) + 1;
        }

        Assert.Equal(6, counts.Count);
        Assert.All(counts.Values, c => Assert.InRange(c, 9_500, 10_500));
    }

    [Fact]
    public void NaiveSwap_ThreeItems_IsMeasurablyBiased()
    {
        Random random = new(27);
        Dictionary<string, int> counts = [];
        for (int t = 0; t < 60_000; t++)
        {
            char[] items = ['A', 'B', 'C'];
            Shuffles.NaiveSwap<char>(items, random);
            string key = new(items);
            counts[key] = counts.GetValueOrDefault(key) + 1;
        }

        Assert.True(counts.Values.Max() - counts.Values.Min() > 2_000);
    }

    [Fact]
    public void Sample_StreamOfTen_EachItemChosenAboutKOverNTimes()
    {
        Random random = new(27);
        int[] hits = new int[10];
        for (int t = 0; t < 30_000; t++)
            foreach (int x in ReservoirSampler.Sample(Enumerable.Range(0, 10), 3, random)) hits[x]++;

        Assert.All(hits, h => Assert.InRange(h, 8_500, 9_500)); // 30,000 * 3 / 10 = 9,000
    }

    [Fact]
    public void Sample_ShorterThanK_ReturnsEverything()
    {
        Assert.Equal([1, 2], ReservoirSampler.Sample([1, 2], 5, new Random(1)));
    }

    [Fact]
    public void WeightedPickerAndAliasTable_FollowWeights()
    {
        (string, double)[] weights = [("Dune", 5), ("Emma", 3), ("Middlemarch", 2)];
        WeightedPicker<string> picker = new(weights);
        AliasTable<string> alias = new(weights);
        Random random = new(27);
        Dictionary<string, int> a = [], b = [];
        for (int t = 0; t < 100_000; t++)
        {
            string x = picker.Pick(random), y = alias.Pick(random);
            a[x] = a.GetValueOrDefault(x) + 1;
            b[y] = b.GetValueOrDefault(y) + 1;
        }

        foreach (Dictionary<string, int> counts in new[] { a, b })
        {
            Assert.InRange(counts["Dune"], 49_000, 51_000);
            Assert.InRange(counts["Emma"], 29_000, 31_000);
            Assert.InRange(counts["Middlemarch"], 19_000, 21_000);
        }
    }

    [Fact]
    public void Bucket_SameInput_SameBucketAndStableValue()
    {
        Assert.Equal(Rollout.Bucket("new-checkout", "user-42"), Rollout.Bucket("new-checkout", "user-42"));
        int enabled = Enumerable.Range(0, 100_000).Count(i => Rollout.IsEnabled("new-checkout", $"user-{i}", 10));
        Assert.InRange(enabled, 9_500, 10_500);
    }
}
