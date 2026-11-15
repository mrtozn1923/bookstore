using BenchmarkDotNet.Attributes;
using Bookstore.Ordering.Functional;
using Bookstore.Ordering.Orders;

namespace Bookstore.Fp.Benchmarks;

[MemoryDiagnoser]
public class ErrorBenchmarks
{
    [Params(0)]
    public int Quantity { get; set; }

    [Benchmark(Baseline = true)]
    public bool ResultFailure() => OrderValidation.ValidateQuantity(Quantity).IsSuccess;

    [Benchmark]
    public bool ThrowAndCatch()
    {
        try
        {
            return CheckQuantity(Quantity) > 0;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static int CheckQuantity(int quantity) =>
        quantity is >= 1 and <= OrderValidation.MaxQuantity
            ? quantity
            : throw new ArgumentOutOfRangeException(nameof(quantity), $"Quantity must be between 1 and {OrderValidation.MaxQuantity}.");
}
