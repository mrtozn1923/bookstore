using BenchmarkDotNet.Attributes;
using Bookstore.Ordering;

namespace Bookstore.Benchmarks;

// Nightly re-pricing of many carts after a price change: is Parallel.For worth it?
[ThreadingDiagnoser]
public class BatchPricingBenchmarks
{
    private PriceLine[][] carts = [];
    private PriceQuote[] quotes = [];

    [Params(100, 10_000)]
    public int Carts { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Random random = new(42);
        carts = [.. Enumerable.Range(0, Carts).Select(_ =>
            Enumerable.Range(0, 1 + random.Next(5)).Select(i => new PriceLine($"979{i:D10}", 249.90m, 1 + random.Next(3))).ToArray())];
        quotes = new PriceQuote[Carts];
    }

    [Benchmark(Baseline = true)]
    public void Sequential()
    {
        for (int i = 0; i < carts.Length; i++)
        {
            quotes[i] = CartPricing.Quote(carts[i], institutional: false);
        }
    }

    [Benchmark]
    public void Parallel() =>
        System.Threading.Tasks.Parallel.For(0, carts.Length, i => quotes[i] = CartPricing.Quote(carts[i], institutional: false));
}
