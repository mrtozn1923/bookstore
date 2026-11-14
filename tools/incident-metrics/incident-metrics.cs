// Per-quarter incident metrics from a CSV, and a check whether the change in mean time to mitigate (MTTM) between
// two quarters is bigger than chance. Usage:
//   dotnet run tools/incident-metrics/incident-metrics.cs -- tools/incident-metrics/sample-history.csv
using System.Globalization;

string path = args.Length > 0 ? args[0] : "tools/incident-metrics/sample-history.csv";
List<Incident> incidents = [.. File.ReadLines(path).Skip(1).Where(l => l.Length > 0).Select(Incident.Parse)];

Console.WriteLine("quarter  n  detect(median)  mitigate(mean / median / max)  by customer  budget spent");
foreach (IGrouping<string, Incident> q in incidents.GroupBy(i => i.Quarter).OrderBy(g => g.Key))
{
    double[] ttm = [.. q.Select(i => i.MinutesToMitigate)];
    Console.WriteLine(
        $"{q.Key}  {q.Count(),1}  {Median([.. q.Select(i => i.MinutesToDetect)]),8:F0} min  " +
        $"{ttm.Average(),10:F1} / {Median(ttm):F0} / {ttm.Max():F0} min  " +
        $"{q.Count(i => i.DetectedBy == "customer"),7} of {q.Count()}  {q.Sum(i => i.BudgetPercent),8}%");
}

// Bootstrap: draw both quarters again from the pooled incidents (as if nothing had changed) and count how often the
// difference between the two means is at least as large as the one we observed.
string[] quarters = [.. incidents.Select(i => i.Quarter).Distinct().Order()];
double[] a = [.. incidents.Where(i => i.Quarter == quarters[0]).Select(i => i.MinutesToMitigate)];
double[] b = [.. incidents.Where(i => i.Quarter == quarters[1]).Select(i => i.MinutesToMitigate)];
double[] pooled = [.. a, .. b];
double observed = Math.Abs(a.Average() - b.Average());
Random random = new(2026);
const int Runs = 100_000;
int asLarge = 0;
for (int run = 0; run < Runs; run++)
{
    double meanA = Enumerable.Range(0, a.Length).Average(_ => pooled[random.Next(pooled.Length)]);
    double meanB = Enumerable.Range(0, b.Length).Average(_ => pooled[random.Next(pooled.Length)]);
    if (Math.Abs(meanA - meanB) >= observed)
    {
        asLarge++;
    }
}
Console.WriteLine($"MTTM {quarters[0]} → {quarters[1]}: {a.Average():F1} → {b.Average():F1} min ({(b.Average() - a.Average()) / a.Average():P0})");
Console.WriteLine($"A difference this large appears by chance in {(double)asLarge / Runs:P1} of {Runs:N0} resampled quarter pairs.");

static double Median(double[] values)
{
    double[] s = [.. values.Order()];
    return s.Length % 2 == 1 ? s[s.Length / 2] : (s[s.Length / 2 - 1] + s[s.Length / 2]) / 2;
}

internal sealed record Incident(string Id, string Quarter, string Severity, DateTime Started, DateTime Detected,
    DateTime Mitigated, string DetectedBy, int BudgetPercent)
{
    public double MinutesToDetect => (Detected - Started).TotalMinutes;
    // Measured from the start of customer impact, not from detection: slow detection is part of the cost.
    public double MinutesToMitigate => (Mitigated - Started).TotalMinutes;

    public static Incident Parse(string line)
    {
        string[] c = line.Split(',');
        DateTime T(string s) => DateTime.ParseExact(s, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
        return new(c[0], c[1], c[2], T(c[3]), T(c[4]), T(c[5]), c[6], int.Parse(c[7], CultureInfo.InvariantCulture));
    }
}
