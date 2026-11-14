namespace Bookstore.Planning;

public enum RiskKind { Technical, Dependency, People, Scope, Regulation }

// ProbabilityBySprint[0] = pre-mortem sonrası ilk durum, [1] = Sprint 24 sonu, [2] = Sprint 25 sonu, [3] = Sprint 26 sonu
public sealed record Risk(string Id, string Title, RiskKind Kind, string Owner, int ImpactDays,
    double[] ProbabilityBySprint, string Trigger, string Response)
{
    public double Exposure(int review) => ProbabilityBySprint[review] * ImpactDays;
}

public static class RiskRegister
{
    static readonly string[] Reviews = ["start", "S24", "S25", "S26"];

    static readonly Risk[] Risks =
    [
        new("R1", "ERP export format unknown", RiskKind.Technical, "Emre", 15, [0.5, 0.15, 0.1, 0.1],
            "No ERP test endpoint by day 5 of Sprint 24", "2-day spike in Sprint 24"),
        new("R2", "E-invoice fields for institutions", RiskKind.Regulation, "Selin", 10, [0.3, 0.3, 0.1, 0.05],
            "Accountant has not confirmed the fields", "Meeting with accounting in Sprint 25"),
        new("R3", "Only Emre knows the pricing code", RiskKind.People, "Ayse", 10, [0.25, 0.15, 0.1, 0.1],
            "Emre on leave or sick", "Pair on every pricing story"),
        new("R4", "Warehouse team adds delivery points", RiskKind.Dependency, "Selin", 8, [0.4, 0.4, 0.2, 0],
            "Field not in their sprint plan", "Agree the API contract, check every week"),
        new("R5", "Approval flow pulled into Release 1", RiskKind.Scope, "Selin", 12, [0.3, 0.3, 0.1, 0.1],
            "School asks for in-app approval", "Hakan confirms with the school"),
        new("R6", "Consent text for institution users", RiskKind.Regulation, "Selin", 5, [0.2, 0.2, 0.2, 0.1],
            "Legal review not booked", "Book legal review early"),
        new("R7", "500-row list paste too slow", RiskKind.Technical, "Ayse", 4, [0.2, 0.05, 0.05, 0.05],
            "p95 above 2 s in test", "Load test the parser in Sprint 24"),
        new("R8", "Year-end leave cuts capacity", RiskKind.People, "Selin", 4, [0, 0, 0.6, 0],
            "Leave calendar shows overlaps", "Plan smaller Sprint 26"),
    ];

    public static void Run()
    {
        Console.WriteLine($"{"Id",-3} {"Risk",-38} {"Kind",-11} {"P",5} {"Days",4} {"Exp",5}  Owner");
        foreach (Risk r in Risks.Where(r => r.ProbabilityBySprint[0] > 0).OrderByDescending(r => r.Exposure(0)))
            Console.WriteLine($"{r.Id,-3} {r.Title,-38} {r.Kind,-11} {r.ProbabilityBySprint[0],5:F2} {r.ImpactDays,4} {r.Exposure(0),5:F1}  {r.Owner}");
        Console.WriteLine($"Total exposure at start: {Risks.Sum(r => r.Exposure(0)):F1} days");

        // Risk burndown: her review'da toplam exposure
        Console.WriteLine();
        Console.WriteLine("Risk burndown (total exposure, days)");
        for (int i = 0; i < Reviews.Length; i++)
        {
            double total = Risks.Sum(r => r.Exposure(i));
            Console.WriteLine($"  {Reviews[i],-5} {total,5:F1} {new string('#', (int)Math.Round(total))}");
        }

        Risk top = Risks.MaxBy(r => r.Exposure(0))!;
        Console.WriteLine();
        Console.WriteLine($"Top risk {top.Id}: trigger \"{top.Trigger}\", response \"{top.Response}\"");
    }
}
