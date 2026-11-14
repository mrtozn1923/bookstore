using Bookstore.Ordering;

namespace Bookstore.Api.Tests;

public sealed class CouponRulesTests
{
    [Fact]
    public void Percent_coupon_takes_a_share_of_the_subtotal()
    {
        Coupon book10 = new() { Code = "BOOK10", Kind = CouponKind.Percent, Value = 10m };

        Assert.Equal(25.00m, CouponRules.CalculateDiscount(book10, 250.00m));
    }

    [Fact]
    public void Percent_discount_is_rounded_to_two_decimals_away_from_zero()
    {
        Coupon book10 = new() { Code = "BOOK10", Kind = CouponKind.Percent, Value = 10m };

        Assert.Equal(33.34m, CouponRules.CalculateDiscount(book10, 333.35m));
    }

    [Fact]
    public void Amount_coupon_takes_a_fixed_amount()
    {
        Coupon save50 = new() { Code = "SAVE50", Kind = CouponKind.Amount, Value = 50m, MinSubtotal = 300m };

        Assert.Equal(50m, CouponRules.CalculateDiscount(save50, 489.00m));
    }
}
