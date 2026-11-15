using Bookstore.Ordering.Carts;

namespace Bookstore.Ordering.Pricing;

public sealed record CheckoutTotals(decimal Subtotal, decimal CouponDiscount, decimal MemberDiscount, decimal Shipping)
{
    public decimal Total => Subtotal - CouponDiscount - MemberDiscount + Shipping;
}

// Functional core: no clock, no console, no static state
public static class CheckoutCalculator
{
    public const decimal MemberDiscountAmount = 15m;
    public const decimal FreeShippingThreshold = 300m;
    public const decimal StandardShippingFee = 49.90m;

    public static CheckoutTotals Calculate(Cart cart, bool isMember, Campaign campaign, DateTimeOffset now)
    {
        IEnumerable<CartLine> lines = cart.Lines.Where(line => line.Quantity > 0);
        decimal subtotal = lines.Sum(line => line.LineTotal);
        decimal coupon = CouponDiscount(subtotal, cart.CouponCode, campaign, now);
        decimal member = isMember ? MemberDiscountAmount : 0m;
        bool hasPrintedBooks = lines.Any(line => !line.IsEbook);
        decimal shipping = hasPrintedBooks && subtotal - coupon - member < FreeShippingThreshold ? StandardShippingFee : 0m;
        return new CheckoutTotals(subtotal, coupon, member, shipping);
    }

    public static decimal CouponDiscount(decimal subtotal, string? couponCode, Campaign campaign, DateTimeOffset now) =>
        couponCode == campaign.Code && campaign.IsActiveAt(now)
            ? Math.Round(subtotal * campaign.Rate, 2, MidpointRounding.AwayFromZero)
            : 0m;
}
