namespace Bookstore.Planning;

public sealed record ConePhase(string Milestone, double Low, double High);

public sealed record PastFeature(string Name, int EstimatedDays, int ActualDays, string Forgotten)
{
    public double Ratio => (double)ActualDays / EstimatedDays;
}

public static class Cone
{
    // McConnell (2006): yetenekli tahmincilerin en iyi durum aralıkları
    static readonly ConePhase[] Phases =
    [
        new("Initial concept", 0.25, 4.0),
        new("Approved product definition", 0.5, 2.0),
        new("Requirements complete", 0.67, 1.5),
        new("UI design complete", 0.8, 1.25),
    ];

    // Bookstore: son 12 feature, tahmin edilen ve gerçekleşen iş günü
    static readonly PastFeature[] History =
    [
        new("GiftCards", 15, 24, "payment provider sandbox"),
        new("Coupons", 10, 13, "code review rounds"),
        new("Returns", 20, 31, "data migration"),
        new("StockReservation", 12, 15, "load test"),
        new("ShipmentTracking", 8, 7, "-"),
        new("CampaignPricing", 10, 16, "data migration"),
        new("InvoicePdf", 5, 9, "deployment"),
        new("SearchRelevance", 15, 18, "code review rounds"),
        new("CartReminder", 6, 6, "-"),
        new("AuthorPages", 4, 5, "deployment"),
        new("ThreeDSecure", 12, 25, "payment provider sandbox"),
        new("AdminReports", 10, 14, "load test"),
    ];

    public static void Run()
    {
        const double gutWeeks = 8; // Emre'nin ilk cevabı: "iki ay yeter"

        Console.WriteLine($"Gut estimate: {gutWeeks} weeks");
        foreach (ConePhase phase in Phases)
            Console.WriteLine($"  {phase.Milestone,-28} {gutWeeks * phase.Low,5:F1} - {gutWeeks * phase.High,5:F1} weeks  (spread {phase.High / phase.Low:F1}x)");

        List<double> ratios = [.. History.Select(f => f.Ratio).Order()];
        int overran = History.Count(f => f.ActualDays > f.EstimatedDays);
        Console.WriteLine();
        Console.WriteLine($"Last {History.Length} features: {overran} overran, total {History.Sum(f => f.EstimatedDays)} estimated vs {History.Sum(f => f.ActualDays)} actual days");
        Console.WriteLine($"actual / estimate  p10 = {Stats.Percentile(ratios, 10):F2}  p50 = {Stats.Percentile(ratios, 50):F2}  p90 = {Stats.Percentile(ratios, 90):F2}");

        // Kendi geçmişimizle düzeltilmiş aralık: p10..p90 = %80
        double low = gutWeeks * Stats.Percentile(ratios, 10);
        double high = gutWeeks * Stats.Percentile(ratios, 90);
        Console.WriteLine($"80% range for an {gutWeeks}-week gut estimate: {low:F1} - {high:F1} weeks");

        Console.WriteLine();
        Console.WriteLine("Forgotten work behind the overruns:");
        foreach (IGrouping<string, PastFeature> g in History.Where(f => f.Forgotten != "-")
                     .GroupBy(f => f.Forgotten).OrderByDescending(g => g.Sum(f => f.ActualDays - f.EstimatedDays)))
            Console.WriteLine($"  {g.Key,-26} {g.Count()} features, +{g.Sum(f => f.ActualDays - f.EstimatedDays)} days");
    }
}
