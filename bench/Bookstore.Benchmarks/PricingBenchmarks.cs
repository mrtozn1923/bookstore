using BenchmarkDotNet.Attributes;
using Bookstore.Ordering;

namespace Bookstore.Benchmarks;

// A named cart shape: ParamsSource shows its ToString() in the results table.
public sealed record CartShape(string Name, int Lines, int QuantityPerLine, bool Institutional)
{
    public override string ToString() => Name;
}

[MemoryDiagnoser]
public class PricingBenchmarks
{
    private PriceLine[] lines = [];

    [ParamsSource(nameof(Carts))]
    public CartShape Cart { get; set; } = null!;

    public static IEnumerable<CartShape> Carts() =>
    [
        new("1 book", 1, 1, false),
        new("12 books", 12, 1, false),
        new("school 40x25", 40, 25, true)
    ];

    [GlobalSetup]
    public void Setup()
    {
        Random random = new(42);
        lines = [.. Enumerable.Range(0, Cart.Lines)
            .Select(i => new PriceLine($"979{i:D10}", Math.Round(99m + (decimal)random.NextDouble() * 900m, 2), Cart.QuantityPerLine))];
    }

    // The first version, before CartPricing: LINQ over the lines, two passes.
    [Benchmark(Baseline = true)]
    public PriceQuote Linq()
    {
        decimal subtotal = lines.Sum(l => OrderItem.CalculateLineTotal(l.UnitPrice, l.Quantity));
        int quantity = lines.Sum(l => l.Quantity);
        decimal discount = Cart.Institutional && quantity >= CartPricing.InstitutionalMinimumQuantity
            ? Math.Round(subtotal * CartPricing.InstitutionalDiscountRate, 2, MidpointRounding.AwayFromZero)
            : 0m;
        return new PriceQuote(subtotal, discount, subtotal - discount);
    }

    [Benchmark]
    public PriceQuote Span() => CartPricing.Quote(lines, Cart.Institutional);
}
