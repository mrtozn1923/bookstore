using System.Diagnostics;
using Bookstore.ConcurrencyLab.Reports;
using Bookstore.ConcurrencyLab.Theory;

namespace Bookstore.ConcurrencyLab.Commands;

// Post 01: interleaving, nondeterminism, Amdahl
public static class BasicsCommands
{
    public static bool Run(string command, string[] args)
    {
        switch (command)
        {
            case "interleavings":
                {
                    string[] a = ["Aread", "Aadd", "Awrite"];
                    string[] b = ["Bread", "Badd", "Bwrite"];
                    List<string[]> all = [.. Interleavings.Enumerate(a, b)];
                    int lost = all.Count(s => Interleavings.RunIncrement(s) == 1);
                    Console.WriteLine($"interleavings: {all.Count}, final=2: {all.Count - lost}, final=1: {lost}");
                    foreach ((int n, int k) in new[] { (2, 3), (2, 10), (3, 10), (4, 10) })
                        Console.WriteLine($"{n} threads x {k} steps -> {Interleavings.Count(n, k):N0}");
                    return true;
                }
            case "observe":
                {
                    int runs = int.Parse(args[1]);
                    Dictionary<string, int> histogram = OrderObserver.Observe(runs);
                    Console.WriteLine($"{runs:N0} runs, {histogram.Count} distinct orders out of 20");
                    foreach ((string order, int count) in histogram.OrderByDescending(p => p.Value))
                        Console.WriteLine($"{order}  {count,7:N0}  {100.0 * count / runs,6:F2}%");
                    return true;
                }
            case "report-phases":
                {
                    Order[] orders = SampleOrders.Create(int.Parse(args[1]), int.Parse(args[2]));
                    MonthlyReport.Build(orders, 1); // warm-up
                    byte[] fingerprints = new byte[orders.Length * 32];
                    byte[] audit = new byte[32];
                    Stopwatch sw = Stopwatch.StartNew();
                    for (int i = 0; i < orders.Length; i++)
                        MonthlyReport.Summarize(orders[i], fingerprints.AsSpan(i * 32, 32));
                    TimeSpan parallelPart = sw.Elapsed;
                    sw.Restart();
                    MonthlyReport.Chain(fingerprints, audit);
                    TimeSpan serialPart = sw.Elapsed;
                    double serial = serialPart / (parallelPart + serialPart);
                    Console.WriteLine($"orders={orders.Length:N0} summarize={parallelPart.TotalMilliseconds:F0} ms chain={serialPart.TotalMilliseconds:F0} ms serial fraction={serial:P1}");
                    foreach (int n in new[] { 2, 4, 8, 10, 64 })
                        Console.WriteLine($"Amdahl n={n,2}: {Amdahl.Speedup(serial, n):F2}x");
                    Console.WriteLine($"limit: {Amdahl.Limit(serial):F2}x");
                    return true;
                }
            case "report":
                {
                    Order[] orders = SampleOrders.Create(int.Parse(args[1]), int.Parse(args[2]));
                    foreach (int n in new[] { 1, 4 })
                    {
                        (decimal revenue, string hash) = MonthlyReport.Build(orders, n);
                        Console.WriteLine($"workers={n} revenue={revenue:N2} TRY audit={hash}");
                    }
                    return true;
                }
        }
        return false;
    }
}
