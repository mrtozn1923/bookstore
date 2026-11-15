namespace Bookstore.Ordering.Tests;

public sealed class CouponCalculatorTests
{
    private static readonly Coupon Kitap10 = new("KITAP10", 10m, 200m, MaxDiscount: 100m);

    // From the story: 10 percent on carts of at least 200 TRY; coupons don't apply to institutional customers
    public static TheoryData<CustomerType, decimal, decimal> StoryCases => new()
    {
        { CustomerType.Individual, 199.99m, 0m },
        { CustomerType.Individual, 200.00m, 20.00m },
        { CustomerType.Guest, 350.00m, 35.00m },
        { CustomerType.Institutional, 350.00m, 0m },
    };

    [Theory]
    [MemberData(nameof(StoryCases))]
    public void Discount_follows_the_story(CustomerType customer, decimal subtotal, decimal expected)
    {
        Assert.Equal(expected, CouponCalculator.Discount(Kitap10, subtotal, customer));
    }

    // From the coverage report: a discount above the cap and a coupon without a cap had never run
    public static TheoryData<decimal, decimal> CapCases => new()
    {
        { 1000.00m, 100.00m },
        { 1000.10m, 100.00m },
        { 2500.00m, 100.00m },
    };

    [Theory]
    [MemberData(nameof(CapCases))]
    public void Discount_is_capped_at_the_maximum(decimal subtotal, decimal expected)
    {
        Assert.Equal(expected, CouponCalculator.Discount(Kitap10, subtotal, CustomerType.Individual));
    }

    [Fact]
    public void Coupon_without_a_maximum_is_not_capped()
    {
        Coupon uncapped = Kitap10 with { MaxDiscount = null };

        Assert.Equal(250.00m, CouponCalculator.Discount(uncapped, 2500.00m, CustomerType.Individual));
    }
}
