using Bookstore.Ordering.Carts;
using Bookstore.Ordering.Pricing;

namespace Bookstore.Ordering.Tests;

public class CheckoutCalculatorTests
{
    private static readonly TimeSpan Istanbul = TimeSpan.FromHours(3);

    private static readonly Campaign Autumn = new(
        "AUTUMN10", 0.10m,
        new DateTimeOffset(2026, 11, 1, 0, 0, 0, Istanbul),
        new DateTimeOffset(2026, 12, 1, 0, 0, 0, Istanbul));

    private static readonly Cart DuneAndEmma = Cart.Empty
        .Add(new CartLine("9780441013593", "Dune", 189.50m, 1, IsEbook: false))
        .Add(new CartLine("9780141439587", "Emma", 59.90m, 1, IsEbook: true))
        with { CouponCode = "AUTUMN10" };

    public static TheoryData<string, DateTimeOffset, decimal> CouponCases => new()
    {
        { "day before start", new DateTimeOffset(2026, 10, 31, 23, 59, 0, Istanbul), 0m },
        { "first minute", new DateTimeOffset(2026, 11, 1, 0, 0, 0, Istanbul), 24.94m },
        { "last minute", new DateTimeOffset(2026, 11, 30, 23, 59, 0, Istanbul), 24.94m },
        { "midnight Istanbul as UTC", new DateTimeOffset(2026, 11, 30, 21, 0, 0, TimeSpan.Zero), 0m }
    };

    [Theory]
    [MemberData(nameof(CouponCases))]
    public void Coupon_applies_only_inside_the_campaign_window(string moment, DateTimeOffset now, decimal expected)
    {
        decimal discount = CheckoutCalculator.CouponDiscount(249.40m, "AUTUMN10", Autumn, now);

        Assert.True(expected == discount, $"{moment}: expected {expected}, got {discount}");
    }

    [Theory]
    [InlineData(true, 259.36)]
    [InlineData(false, 274.36)]
    public void Total_matches_the_legacy_checkout(bool isMember, decimal expected)
    {
        CheckoutTotals totals = CheckoutCalculator.Calculate(DuneAndEmma, isMember, Autumn, new DateTimeOffset(2026, 11, 15, 12, 0, 0, Istanbul));

        Assert.Equal(expected, totals.Total);
    }

    [Fact]
    public void Same_input_gives_the_same_result()
    {
        DateTimeOffset now = new(2026, 11, 15, 12, 0, 0, Istanbul);

        Assert.Equal(
            CheckoutCalculator.Calculate(DuneAndEmma, true, Autumn, now),
            CheckoutCalculator.Calculate(DuneAndEmma, true, Autumn, now));
    }
}
