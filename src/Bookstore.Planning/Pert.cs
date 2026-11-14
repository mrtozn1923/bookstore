namespace Bookstore.Planning;

public sealed record WorkPackage(string Name, double Optimistic, double MostLikely, double Pessimistic)
{
    public double PertMean => (Optimistic + 4 * MostLikely + Pessimistic) / 6;
    public double PertSigma => (Pessimistic - Optimistic) / 6;

    // Triangular dağılımdan örnek (inverse CDF)
    public double Sample(Random rng)
    {
        double u = rng.NextDouble();
        (double a, double m, double b) = (Optimistic, MostLikely, Pessimistic);
        double split = (m - a) / (b - a);
        return u < split
            ? a + Math.Sqrt(u * (b - a) * (m - a))
            : b - Math.Sqrt((1 - u) * (b - a) * (b - m));
    }
}

public sealed record PastEpic(string Name, double EstimatedWeeks, double ActualWeeks)
{
    public double Ratio => ActualWeeks / EstimatedWeeks;
}

public static class Pert
{
    // "Listeyle toplu sipariş" dilimi, developer-day cinsinden O / M / P
    static readonly WorkPackage[] Packages =
    [
        new("Parse pasted list (ISBN-10/13)", 1, 2, 4),
        new("ISBN validation + catalog lookup", 1, 2, 3),
        new("Match rows without ISBN by title", 2, 4, 10),
        new("Validation result screen", 2, 3, 6),
        new("Institutional discount rule", 0.5, 1, 2),
        new("Stock check for large quantities", 1, 2, 5),
        new("Save as draft", 1, 2, 4),
        new("Submit with PO number + e-mail", 1, 2, 4),
        new("Tests, review, deployment", 2, 3, 6),
    ];

    // Dışarıdan bakış: Bookstore'un son 8 epic'i, ilk tahmin ve gerçekleşen
    static readonly PastEpic[] Epics =
    [
        new("CheckoutV2", 6, 9),
        new("Returns", 4, 6.2),
        new("CampaignEngine", 5, 8),
        new("MobileWeb", 8, 11),
        new("SearchRelevance", 3, 3.6),
        new("GiftCards", 3, 4.8),
        new("AdminReports", 2, 2.8),
        new("StockReservation", 2.5, 3),
    ];

    public static void Run()
    {
        Console.WriteLine($"{"Work package",-34} {"O",4} {"M",4} {"P",4} {"PERT",6}");
        foreach (WorkPackage w in Packages)
            Console.WriteLine($"{w.Name,-34} {w.Optimistic,4} {w.MostLikely,4} {w.Pessimistic,4} {w.PertMean,6:F2}");

        double sumMostLikely = Packages.Sum(w => w.MostLikely);
        double mean = Packages.Sum(w => w.PertMean);
        double sigma = Math.Sqrt(Packages.Sum(w => w.PertSigma * w.PertSigma)); // bağımsızlık varsayımı
        Console.WriteLine();
        Console.WriteLine($"Sum of most likely   : {sumMostLikely:F1} days");
        Console.WriteLine($"Sum of pessimistic   : {Packages.Sum(w => w.Pessimistic):F1} days");
        Console.WriteLine($"PERT total           : {mean:F1} days, sigma {sigma:F1}");
        Console.WriteLine($"PERT 85% (normal)    : {mean + 1.04 * sigma:F1} days");

        const int Runs = 10_000;
        Random rng = new(2026);
        List<double> totals = [.. Enumerable.Range(0, Runs).Select(_ => Packages.Sum(w => w.Sample(rng))).Order()];
        Console.WriteLine();
        Console.WriteLine($"Simulation, {Runs:N0} runs (triangular):");
        foreach (int p in new[] { 10, 50, 85, 95 })
            Console.WriteLine($"  p{p,-2} {Stats.Percentile(totals, p),5:F1} days");
        Console.WriteLine($"  P(total <= sum of most likely) = {totals.Count(t => t <= sumMostLikely) * 100.0 / Runs:F1}%");

        // Reference class: geçmiş epic'lerde gerçekleşen / tahmin oranı
        List<double> ratios = [.. Epics.Select(e => e.Ratio).Order()];
        Console.WriteLine();
        Console.WriteLine($"Reference class: {Epics.Length} past epics, actual / estimate");
        Console.WriteLine($"  ratios: {string.Join(" ", ratios.Select(r => r.ToString("F2")))}");
        Console.WriteLine($"  p50 uplift x{Stats.Percentile(ratios, 50):F2}, p80 uplift x{Stats.Percentile(ratios, 80):F2}");
        Console.WriteLine($"  sum of most likely x p80 uplift = {sumMostLikely * Stats.Percentile(ratios, 80):F1} days");
    }
}
