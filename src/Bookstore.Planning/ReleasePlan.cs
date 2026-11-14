namespace Bookstore.Planning;

public static class ReleasePlan
{
    public static readonly DateOnly FirstSprintStart = new(2026, 11, 23); // Sprint 24, pazartesi
    public static readonly DateOnly Target = new(2027, 3, 1);

    // n. sprint'in son iş günü (2 haftalık sprint, cuma biter)
    public static DateOnly SprintEnd(int n) => FirstSprintStart.AddDays(14 * (n - 1) + 11);

    public static (double Low, double Mean, double High) VelocityRange(Sprint[] history)
    {
        List<int> v = [.. history.Select(s => s.Velocity).Order()];
        return (v.Take(3).Average(), v.Average(), v.TakeLast(3).Average());
    }

    public static void Run()
    {
        Sprint[] history = SprintHistory.Last10;
        int scope = SprintHistory.Release1.Sum();
        (double low, double mean, double high) = VelocityRange(history);

        Console.WriteLine($"Scope: {scope} points. Velocity range (last {history.Length} sprints): worst-3 {low:F1}, mean {mean:F1}, best-3 {high:F1}");
        foreach ((string name, double v) in new[] { ("worst-3", low), ("mean", mean), ("best-3", high) })
        {
            int sprints = (int)Math.Ceiling(scope / v);
            Console.WriteLine($"  {name,-8} {sprints} sprints -> done by {SprintEnd(sprints):yyyy-MM-dd}");
        }

        // Sabit tarih: 1 Mart'tan önce biten sprint sayısı ve sığan kapsam
        int fixedSprints = Enumerable.Range(1, 20).Last(n => SprintEnd(n) < Target);
        (string Name, int Points)[] moscow = [("Must", 96), ("Should", 22), ("Could", 12)];
        Console.WriteLine();
        Console.WriteLine($"Fixed date {Target:yyyy-MM-dd}: {fixedSprints} sprints (last ends {SprintEnd(fixedSprints):yyyy-MM-dd})");
        foreach ((string name, double v) in new[] { ("worst-3", low), ("mean", mean), ("best-3", high) })
        {
            double capacity = fixedSprints * v;
            int cumulative = 0;
            string fits = string.Join(", ", moscow.TakeWhile(m => (cumulative += m.Points) <= capacity).Select(m => m.Name));
            Console.WriteLine($"  {name,-8} ~{capacity:F0} points -> {fits}");
        }

        // Akış bakışı: geçmiş sprint'lerden örneklem alan Monte Carlo (seed sabit)
        const int Runs = 10_000;
        Random rng = new(2026);
        int stories = SprintHistory.Release1.Length;
        List<int> byPoints = [.. Enumerable.Range(0, Runs).Select(_ => SprintsUntil(scope, () => history[rng.Next(history.Length)].Velocity)).Order()];
        List<int> byCount = [.. Enumerable.Range(0, Runs).Select(_ => SprintsUntil(stories, () => history[rng.Next(history.Length)].Throughput)).Order()];

        Console.WriteLine();
        Console.WriteLine($"Monte Carlo, {Runs:N0} runs: sprints needed (finish date)");
        Console.WriteLine($"  {"",4} {"by points (130)",-24} {"by count (30 stories)",-24}");
        foreach (int p in new[] { 50, 85, 95 })
        {
            int a = Stats.Percentile(byPoints, p), b = Stats.Percentile(byCount, p);
            Console.WriteLine($"  p{p}  {a} ({SprintEnd(a):yyyy-MM-dd}){"",8} {b} ({SprintEnd(b):yyyy-MM-dd})");
        }
        Console.WriteLine($"  P(done before {Target:yyyy-MM-dd}): by points {byPoints.Count(s => s <= fixedSprints) * 100.0 / Runs:F0}%, by count {byCount.Count(s => s <= fixedSprints) * 100.0 / Runs:F0}%");
    }

    static int SprintsUntil(int remaining, Func<int> nextSprint)
    {
        int sprints = 0;
        while (remaining > 0)
        {
            remaining -= nextSprint();
            sprints++;
        }
        return sprints;
    }
}
