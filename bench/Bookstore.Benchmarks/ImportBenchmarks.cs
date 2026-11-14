using System.Globalization;
using BenchmarkDotNet.Attributes;

namespace Bookstore.Benchmarks;

// Supplier price lists arrive as "isbn;price" lines; some prices are malformed.
[MemoryDiagnoser]
[ExceptionDiagnoser]
public class ImportBenchmarks
{
    private string[] rows = [];

    [Params(0, 10)]
    public int BadPercent { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Random random = new(42);
        rows = [.. Enumerable.Range(0, 1_000).Select(i =>
            $"979{i:D10};{(random.Next(100) < BadPercent ? "n/a" : (99 + i % 900).ToString(CultureInfo.InvariantCulture) + ".90")}")];
    }

    [Benchmark(Baseline = true)]
    public decimal ParseWithCatch()
    {
        decimal total = 0m;
        foreach (string row in rows)
        {
            try
            {
                total += decimal.Parse(row.AsSpan(row.IndexOf(';') + 1), CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                // skip the malformed row
            }
        }
        return total;
    }

    [Benchmark]
    public decimal TryParse()
    {
        decimal total = 0m;
        foreach (string row in rows)
        {
            if (decimal.TryParse(row.AsSpan(row.IndexOf(';') + 1), CultureInfo.InvariantCulture, out decimal price))
            {
                total += price;
            }
        }
        return total;
    }
}
