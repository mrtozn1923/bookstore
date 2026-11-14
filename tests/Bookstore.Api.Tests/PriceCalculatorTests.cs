using Bookstore.Api.Services;
using Xunit;

namespace Bookstore.Api.Tests;

public class PriceCalculatorTests
{
    [Theory]
    [InlineData(450, 10, 405)]
    [InlineData(280, 0, 280)]
    [InlineData(99.99, 15, 84.99)]
    public void ApplyDiscount_ValidPercent_ReturnsRoundedPrice(decimal price, int percent, decimal expected)
    {
        var result = PriceCalculator.ApplyDiscount(price, percent);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(91)]
    public void ApplyDiscount_PercentOutOfRange_Throws(int percent)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PriceCalculator.ApplyDiscount(100m, percent));
    }
}
