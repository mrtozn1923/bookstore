namespace Bookstore.Shipping;

public enum Destination
{
    Domestic,
    International,
}

public sealed record ShippingRequest(
    bool IsPlusMember, decimal DiscountedSubtotal, Destination Destination, bool HasFreeShippingCoupon);

public sealed record ShippingQuote(decimal Fee, bool CouponConsumed, bool CouponRejected);

public static class ShippingFeeCalculator
{
    public const decimal DomesticFee = 49.90m;
    public const decimal InternationalFee = 349.90m;

    private sealed record Rule(Func<ShippingRequest, bool> Applies, Func<ShippingRequest, ShippingQuote> Quote);

    // The first matching rule wins; international rules come before free shipping rules
    private static readonly Rule[] Rules =
    [
        new(r => r.Destination == Destination.International && r.IsPlusMember,
            r => new(InternationalFee / 2, false, r.HasFreeShippingCoupon)),
        new(r => r.Destination == Destination.International,
            r => new(InternationalFee, false, r.HasFreeShippingCoupon)),
        new(r => r.IsPlusMember, _ => new(0m, false, false)),
        new(r => FreeShippingPolicy.Qualifies(r.DiscountedSubtotal), _ => new(0m, false, false)),
        new(r => r.HasFreeShippingCoupon, _ => new(0m, true, false)),
        new(_ => true, _ => new(DomesticFee, false, false)),
    ];

    public static ShippingQuote Quote(ShippingRequest request) =>
        Rules.First(rule => rule.Applies(request)).Quote(request);
}
