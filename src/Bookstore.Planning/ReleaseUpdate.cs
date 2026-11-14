namespace Bookstore.Planning;

public sealed record SprintResult(int Number, int DonePoints, int ScopeAtEnd);

public static class ReleaseUpdate
{
    // Kurumsal Sipariş Release 1: başladığından beri her sprint sonu
    static readonly SprintResult[] Actuals =
    [
        new(24, 19, 133),
        new(25, 17, 138),
        new(26, 11, 142),
    ];

    const int PlannedScope = 130;
    const int MustAtStart = 96;

    public static void Run()
    {
        // Plan (ortalama 20) ile gerçekleşenin burn-up karşılaştırması
        Console.WriteLine("Sprint  planned  done  scope");
        int done = 0;
        for (int i = 0; i < Actuals.Length; i++)
        {
            done += Actuals[i].DonePoints;
            Console.WriteLine($"  {Actuals[i].Number,4}  {20 * (i + 1),7}  {done,4}  {Actuals[i].ScopeAtEnd,5}");
        }

        int scope = Actuals[^1].ScopeAtEnd;
        int remaining = scope - done;
        double growth = (double)(scope - PlannedScope) / Actuals.Length;
        Console.WriteLine($"Remaining {remaining} points, scope growth {growth:F1} points/sprint");

        // Velocity aralığını yeni sprint'lerle güncelle: yine son 10 sprint (burada yalnız Velocity kullanılıyor)
        Sprint[] last10 = [.. SprintHistory.Last10.Concat(Actuals.Select(a => new Sprint(a.Number, [a.DonePoints]))).TakeLast(10)];
        (double low, double mean, double high) = ReleasePlan.VelocityRange(last10);
        Console.WriteLine($"Velocity range now (S{last10[0].Number}-S{last10[^1].Number}): worst-3 {low:F1}, mean {mean:F1}, best-3 {high:F1}");

        int used = Actuals.Length;
        Console.WriteLine();
        Console.WriteLine("Option A, keep full scope:");
        foreach ((string name, double v) in new[] { ("worst-3", low), ("mean", mean), ("best-3", high) })
        {
            int flat = (int)Math.Ceiling(remaining / v);
            int growing = (int)Math.Ceiling(remaining / (v - growth));
            Console.WriteLine($"  {name,-8} {ReleasePlan.SprintEnd(used + flat):yyyy-MM-dd}  (if scope keeps growing: {ReleasePlan.SprintEnd(used + growing):yyyy-MM-dd})");
        }

        // B: 1 Mart'a kadar yalnız Must; yeni çıkan regülasyon işleri de Must
        int sprintsLeft = Enumerable.Range(1, 20).Last(n => ReleasePlan.SprintEnd(n) < ReleasePlan.Target) - used;
        int mustRemaining = MustAtStart + (scope - PlannedScope) - done;
        Console.WriteLine();
        Console.WriteLine($"Option B, Must only by {ReleasePlan.Target:yyyy-MM-dd}: {mustRemaining} points in {sprintsLeft} sprints");
        foreach ((string name, double v) in new[] { ("worst-3", low), ("mean", mean), ("best-3", high) })
            Console.WriteLine($"  {name,-8} capacity ~{sprintsLeft * v:F0} -> {(sprintsLeft * v >= mustRemaining ? "fits" : "does not fit")}");
    }
}
