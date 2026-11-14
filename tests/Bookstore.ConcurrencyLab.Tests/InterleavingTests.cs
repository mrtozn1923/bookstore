using Bookstore.ConcurrencyLab.Theory;

namespace Bookstore.ConcurrencyLab.Tests;

public class InterleavingTests
{
    [Fact]
    public void Two_threads_with_three_steps_have_twenty_interleavings()
    {
        List<string[]> all = [.. Interleavings.Enumerate(["A1", "A2", "A3"], ["B1", "B2", "B3"])];

        Assert.Equal(20, all.Count);
        Assert.Equal(20, all.Select(s => string.Concat(s)).Distinct().Count());
        Assert.Equal(20, (int)Interleavings.Count(2, 3));
    }

    [Fact]
    public void Only_serial_schedules_keep_both_increments()
    {
        List<string[]> all = [.. Interleavings.Enumerate(["Aread", "Aadd", "Awrite"], ["Bread", "Badd", "Bwrite"])];

        Assert.Equal(2, all.Count(s => Interleavings.RunIncrement(s) == 2));
        Assert.Equal(18, all.Count(s => Interleavings.RunIncrement(s) == 1));
    }

    [Theory]
    [InlineData(0.10, 64, 8.77)]
    [InlineData(0.10, 4, 3.08)]
    [InlineData(0.05, 1024, 19.64)]
    public void Amdahl_speedup_matches_formula(double serial, int cores, double expected) =>
        Assert.Equal(expected, Amdahl.Speedup(serial, cores), 2);

    [Fact]
    public void Gustafson_scaled_speedup_grows_with_cores() =>
        Assert.Equal(57.7, Amdahl.ScaledSpeedup(0.10, 64), 1);
}
