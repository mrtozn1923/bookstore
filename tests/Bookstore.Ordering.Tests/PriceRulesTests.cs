using Bookstore.Ordering.Functional;
using Bookstore.Ordering.Pricing;

namespace Bookstore.Ordering.Tests;

public class PriceRulesTests
{
    [Theory]
    [InlineData(249.40, 209.46)]
    [InlineData(100.00, 75.00)]
    [InlineData(10.00, 0.00)]
    public void Coupon_is_applied_before_the_member_discount(decimal subtotal, decimal expected)
    {
        Func<decimal, decimal> checkoutRules = PriceRules.Combine(
            [PriceRules.PercentOff(0.10m), PriceRules.AmountOff(15m), PriceRules.RoundToKurus]);

        Assert.Equal(expected, checkoutRules(subtotal));
    }

    [Fact]
    public void Rule_order_changes_the_result()
    {
        Func<decimal, decimal> couponFirst = PriceRules.PercentOff(0.10m).Then(PriceRules.AmountOff(15m));
        Func<decimal, decimal> memberFirst = PriceRules.AmountOff(15m).Then(PriceRules.PercentOff(0.10m));

        Assert.NotEqual(couponFirst(249.40m), memberFirst(249.40m));
    }

    [Theory]
    [InlineData(0.00)]
    [InlineData(249.40)]
    public void Combining_no_rules_returns_the_amount_unchanged(decimal amount) =>
        Assert.Equal(amount, PriceRules.Combine([])(amount));
}
