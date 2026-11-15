using System.Security.Cryptography;
using Algorithms.Randomness;

namespace Algorithms.Demos.Demos;

public static class RandomnessDemos
{
    public static void ShuffleBias()
    {
        const int trials = 600_000;
        Random random = new(27);
        Report("Fisher-Yates", trials, items => Shuffles.FisherYates<char>(items, random));
        Report("naive swap", trials, items => Shuffles.NaiveSwap<char>(items, random));
        Report("OrderBy(Guid)", trials, items => items.OrderBy(_ => Guid.NewGuid()).ToArray().CopyTo(items, 0));
        Report("random comparer", trials, items => Shuffles.SortWithRandomComparer(items, random));
        Report("Random.Shuffle", trials, items => random.Shuffle(items));
    }

    private static void Report(string name, int trials, Action<char[]> shuffle)
    {
        SortedDictionary<string, int> counts = new(StringComparer.Ordinal);
        for (int t = 0; t < trials; t++)
        {
            char[] items = ['A', 'B', 'C'];
            shuffle(items);
            string key = new(items);
            counts[key] = counts.GetValueOrDefault(key) + 1;
        }

        Console.WriteLine($"{name,-16} " + string.Join(" ", counts.Select(kv => $"{kv.Key}={100.0 * kv.Value / trials:F1}%")));
    }

    public static void Reservoir()
    {
        // A log we can only read once, line by line, without knowing its length
        IEnumerable<string> LogLines()
        {
            for (int i = 1; i <= 5_000_000; i++) yield return $"line {i}";
        }

        string[] sample = ReservoirSampler.Sample(LogLines(), 5, new Random(27));
        Console.WriteLine("sample: " + string.Join(" | ", sample));
    }

    public static void BookOfTheDay()
    {
        (string, double)[] candidates = [("Dune", 50), ("Emma", 30), ("Middlemarch", 15), ("The Hobbit", 5)];
        WeightedPicker<string> picker = new(candidates);
        AliasTable<string> alias = new(candidates);
        Random random = new(27);
        Dictionary<string, int> byPicker = [], byAlias = [];
        for (int day = 0; day < 100_000; day++)
        {
            string a = picker.Pick(random), b = alias.Pick(random);
            byPicker[a] = byPicker.GetValueOrDefault(a) + 1;
            byAlias[b] = byAlias.GetValueOrDefault(b) + 1;
        }

        foreach ((string title, double weight) in candidates)
            Console.WriteLine($"{title,-12} weight {weight,2}%  cumulative {byPicker[title] / 1000.0,5:F1}%  alias {byAlias[title] / 1000.0,5:F1}%");
    }

    public static void Rollouts()
    {
        Console.WriteLine($"user-42 new-checkout bucket: {Rollout.Bucket("new-checkout", "user-42")}");
        Console.WriteLine($"user-42 dark-mode bucket:    {Rollout.Bucket("dark-mode", "user-42")}");
        int checkout = 0, both = 0;
        for (int i = 0; i < 100_000; i++)
        {
            bool a = Rollout.IsEnabled("new-checkout", $"user-{i}", 10);
            bool b = Rollout.IsEnabled("dark-mode", $"user-{i}", 10);
            if (a) checkout++;
            if (a && b) both++;
        }

        Console.WriteLine($"new-checkout at 10%: {checkout:N0} of 100,000 users; also in dark-mode at 10%: {both:N0}");
        Console.WriteLine($"\"user-42\".GetHashCode() in this process: {"user-42".GetHashCode()}");
    }

    public static void Pitfalls()
    {
        int distinct = Enumerable.Range(0, 1_000).Select(_ => new Random().Next()).Distinct().Count();
        Console.WriteLine($"1,000 x new Random() in a loop: {distinct} distinct first values");

        Random shared = new(27);
        long zeros = 0;
        Parallel.For(0, 10_000_000, _ =>
        {
            if (shared.Next() == 0) Interlocked.Increment(ref zeros);
        });
        int zerosAfter = Enumerable.Range(0, 1_000).Count(_ => shared.Next() == 0);
        Console.WriteLine($"seeded Random shared by threads: {zeros:N0} of 10,000,000 calls returned 0; afterwards {zerosAfter} of 1,000");

        long sharedZeros = 0;
        Parallel.For(0, 10_000_000, _ =>
        {
            if (Random.Shared.Next() == 0) Interlocked.Increment(ref sharedZeros);
        });
        Console.WriteLine($"Random.Shared from threads: {sharedZeros:N0} zeros");

        Console.WriteLine($"Random(12345) token: {new Random(12345).NextInt64():x16}, again: {new Random(12345).NextInt64():x16}");
        Console.WriteLine($"RandomNumberGenerator token length: {RandomNumberGenerator.GetHexString(32).Length} hex chars");
    }
}
