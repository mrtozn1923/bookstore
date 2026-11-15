namespace Bookstore.Shipping.Tests;

public sealed class ShippingFeeCalculatorTests
{
    private const bool Plus = true;
    private const bool Coupon = true;

    // Collapsed decision table: one test per column; representative values for "-" conditions
    public static IEnumerable<TheoryDataRow<bool, decimal, Destination, bool, ShippingQuote>> DecisionTable =>
    [
        new(Plus, 120.00m, Destination.Domestic, Coupon, new(0m, false, false)) { Label = "R1" },
        new(!Plus, 500.00m, Destination.Domestic, Coupon, new(0m, false, false)) { Label = "R2" },
        new(!Plus, 499.99m, Destination.Domestic, Coupon, new(0m, true, false)) { Label = "R3" },
        new(!Plus, 499.99m, Destination.Domestic, !Coupon, new(49.90m, false, false)) { Label = "R4" },
        new(Plus, 650.00m, Destination.International, !Coupon, new(174.95m, false, false)) { Label = "R5" },
        new(Plus, 120.00m, Destination.International, Coupon, new(174.95m, false, true)) { Label = "R6" },
        new(!Plus, 650.00m, Destination.International, !Coupon, new(349.90m, false, false)) { Label = "R7" },
        new(!Plus, 120.00m, Destination.International, Coupon, new(349.90m, false, true)) { Label = "R8" },
    ];

    [Theory]
    [MemberData(nameof(DecisionTable))]
    public void Shipping_quote_follows_the_decision_table(
        bool isPlusMember, decimal subtotal, Destination destination, bool hasCoupon, ShippingQuote expected)
    {
        ShippingQuote quote = ShippingFeeCalculator.Quote(new(isPlusMember, subtotal, destination, hasCoupon));

        Assert.Equal(expected, quote);
    }
}
