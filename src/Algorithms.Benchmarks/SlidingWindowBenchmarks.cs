using Algorithms.Intervals;
using BenchmarkDotNet.Attributes;

namespace Algorithms.Benchmarks;

[MemoryDiagnoser]
public class SlidingWindowBenchmarks
{
    private decimal[] _prices = [];

    [Params(30, 365)]
    public int Window { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Random random = new(25);
        _prices = [.. Enumerable.Range(0, 100_000).Select(_ => (decimal)random.Next(50, 200))];
    }

    [Benchmark(Baseline = true)]
    public decimal[] Naive() => SlidingWindow.MinimumsNaive(_prices, Window);

    [Benchmark]
    public decimal[] MonotonicDeque() => SlidingWindow.Minimums(_prices, Window);
}
