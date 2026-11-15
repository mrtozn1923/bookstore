namespace Bookstore.Shipping.Tests;

public sealed class FreeShippingPolicyTests
{
    // 3-value BVA: boundary values 499.99 and 500.00, neighbors 499.98 and 500.01 (smallest step: 1 kurus)
    public static TheoryData<decimal, bool> ThresholdBoundaries => new()
    {
        { 0.00m, false },
        { 499.98m, false },
        { 499.99m, false },
        { 500.00m, true },
        { 500.01m, true },
    };

    [Theory]
    [MemberData(nameof(ThresholdBoundaries))]
    public void Free_shipping_starts_at_exactly_500_try(decimal discountedSubtotal, bool expected)
    {
        Assert.Equal(expected, FreeShippingPolicy.Qualifies(discountedSubtotal));
    }
}
