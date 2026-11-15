using Algorithms.Probabilistic;

namespace Algorithms.Demos.Demos;

public static class ProbabilisticDemos
{
    public static void Bloom()
    {
        const int registered = 1_000_000;
        long before = GC.GetTotalMemory(forceFullCollection: true);
        HashSet<string> exact = [.. Enumerable.Range(0, registered).Select(i => $"user{i}@example.com")];
        long hashSetBytes = GC.GetTotalMemory(forceFullCollection: true) - before;

        BloomFilter filter = BloomFilter.ForCapacity(registered, 0.01);
        foreach (string email in exact) filter.Add(email);
        Console.WriteLine($"bloom: m={filter.BitCount:N0} bits ({filter.SizeInBytes / 1024.0 / 1024:F2} MB), k={filter.HashCount}");
        Console.WriteLine($"HashSet<string> with the e-mails: {hashSetBytes / 1024.0 / 1024:F1} MB");

        int falseNegatives = exact.Count(e => !filter.MightContain(e));
        int falsePositives = Enumerable.Range(0, 1_000_000).Count(i => filter.MightContain($"new{i}@example.com"));
        Console.WriteLine($"false negatives: {falseNegatives}");
        Console.WriteLine($"false positives: {falsePositives:N0} / 1,000,000 = {falsePositives / 1e6:P2} (formula {filter.ExpectedFalsePositiveRate(registered):P2})");

        for (int i = registered; i < 3 * registered; i++) filter.Add($"user{i}@example.com"); // three times the planned capacity
        int overfull = Enumerable.Range(0, 1_000_000).Count(i => filter.MightContain($"new{i}@example.com"));
        Console.WriteLine($"after 3,000,000 adds: {overfull / 1e6:P2} (formula {filter.ExpectedFalsePositiveRate(3 * registered):P2})");
    }

    public static void TopViewed()
    {
        // Zipf-like views: book r gets views proportional to 1 / r
        const int books = 100_000;
        double[] cumulative = new double[books];
        double total = 0;
        for (int r = 0; r < books; r++) cumulative[r] = total += 1.0 / (r + 1);
        Random random = new(26);
        CountMinSketch sketch = CountMinSketch.ForError(0.001, 0.01);
        Dictionary<string, long> exact = [];
        for (int v = 0; v < 2_000_000; v++)
        {
            int index = Array.BinarySearch(cumulative, random.NextDouble() * total);
            if (index < 0) index = ~index;
            string isbn = $"978{index:D10}";
            sketch.Add(isbn);
            exact[isbn] = exact.GetValueOrDefault(isbn) + 1;
        }

        Console.WriteLine($"sketch: {sketch.Width} x {sketch.Depth} counters = {sketch.SizeInBytes / 1024} KB, distinct books viewed: {exact.Count:N0}");
        foreach (KeyValuePair<string, long> kv in exact.OrderByDescending(kv => kv.Value).Take(3))
            Console.WriteLine($"{kv.Key}: exact {kv.Value,7:N0}, sketch {sketch.Estimate(kv.Key),7:N0}");
        long[] errors = [.. exact.Select(kv => sketch.Estimate(kv.Key) - kv.Value)];
        Console.WriteLine($"over-count: max {errors.Max():N0}, average {errors.Average():F1}, bound epsilon*N = {0.001 * sketch.TotalCount:N0}");
        KeyValuePair<string, long> rare = exact.Where(kv => kv.Value == 1).First();
        Console.WriteLine($"a book viewed once: exact 1, sketch {sketch.Estimate(rare.Key)}");
    }

    public static void UniqueVisitors()
    {
        Random random = new(26);
        HyperLogLog monday = new(14), tuesday = new(14);
        HashSet<string> exactMonday = [], exactBoth = [];
        for (int i = 0; i < 1_500_000; i++)
        {
            string visitor = $"visitor-{random.Next(400_000)}";
            monday.Add(visitor);
            exactMonday.Add(visitor);
            exactBoth.Add(visitor);
        }

        for (int i = 0; i < 1_500_000; i++)
        {
            string visitor = $"visitor-{200_000 + random.Next(400_000)}";
            tuesday.Add(visitor);
            exactBoth.Add(visitor);
        }

        Console.WriteLine($"registers: {monday.RegisterCount:N0} (1 byte each), standard error {monday.StandardError:P2}");
        Console.WriteLine($"monday unique: exact {exactMonday.Count:N0}, HLL {monday.Count():N0}");
        monday.Merge(tuesday);
        Console.WriteLine($"monday + tuesday: exact {exactBoth.Count:N0}, HLL {monday.Count():N0}");
    }
}
