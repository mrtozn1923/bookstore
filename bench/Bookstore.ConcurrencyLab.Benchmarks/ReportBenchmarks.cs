using BenchmarkDotNet.Attributes;
using Bookstore.ConcurrencyLab.Reports;

namespace Bookstore.ConcurrencyLab.Benchmarks;

[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class ReportBenchmarks
{
    private Order[] _orders = [];

    [Params(50, 200_000)]
    public int Orders { get; set; }

    [Params(1, 2, 4, 8, 10)]
    public int Workers { get; set; }

    [GlobalSetup]
    public void Setup() => _orders = SampleOrders.Create(Orders, linesPerOrder: 22);

    [Benchmark(Baseline = true)]
    public decimal SingleChain() => MonthlyReport.Build(_orders, Workers).Revenue;

    [Benchmark]
    public decimal PerDayChain() => MonthlyReport.BuildPerDay(_orders, Workers).Revenue;
}
