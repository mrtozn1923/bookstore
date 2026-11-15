using BenchmarkDotNet.Attributes;
using Bookstore.Ordering.Carts;

namespace Bookstore.Fp.Benchmarks;

[MemoryDiagnoser]
public class SubtotalBenchmarks
{
    private CartLine[] _lines = [];

    [Params(3, 1000)]
    public int Lines { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _lines = Enumerable.Range(0, Lines)
            .Select(i => new CartLine($"978{i:D10}", $"Book {i}", 100m + i % 50, i % 7 == 0 ? 0 : 1 + i % 3, i % 2 == 0))
            .ToArray();
        if (Linq() != Loop())
            throw new InvalidOperationException("The two implementations disagree.");
    }

    [Benchmark(Baseline = true)]
    public decimal Loop()
    {
        decimal subtotal = 0m;
        foreach (CartLine line in _lines)
        {
            if (line.Quantity > 0)
                subtotal += line.LineTotal;
        }
        return subtotal;
    }

    [Benchmark]
    public decimal Linq() => _lines.Where(line => line.Quantity > 0).Sum(line => line.LineTotal);
}
