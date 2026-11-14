using Bookstore.ConcurrencyLab.Hardware;
using Bookstore.ConcurrencyLab.MemoryModel;

namespace Bookstore.ConcurrencyLab.Tests;

public class MemoryModelTests
{
    [Fact]
    public void Loop_reading_the_flag_with_Volatile_Read_stops()
    {
        (bool ended, _) = StopFlagCheck.Run(w => w.RunVolatileRead(), TimeSpan.FromMilliseconds(50), TimeSpan.FromSeconds(5));

        Assert.True(ended);
    }

    [Theory]
    [InlineData("reference")]
    [InlineData("lazy")]
    public void Correct_double_checks_always_return_the_full_price_list(string kind)
    {
        Func<IPriceListCache> create = kind == "lazy" ? () => new LazyCache() : () => new ReferenceCheckedCache();

        long[,] outcomes = new LitmusRunner(new DoubleCheckTest(kind, create)).Run(200_000);

        Assert.Equal(200_000, outcomes[1, 1]);
    }

    [Fact]
    public void Object_published_through_a_plain_field_is_seen_complete()
    {
        long[,] outcomes = new LitmusRunner(new PublicationTest()).Run(200_000);

        Assert.Equal(0, outcomes[0, 2]);
    }
}
