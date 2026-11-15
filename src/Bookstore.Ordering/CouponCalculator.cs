namespace Bookstore.Ordering;

public sealed record Coupon(string Code, decimal Percent, decimal MinimumSubtotal, decimal? MaxDiscount = null);

public static class CouponCodes
{
    public static string Normalize(string code) => code.Trim().ToUpperInvariant();
}

public static class CouponCalculator
{
    public static decimal Discount(Coupon coupon, decimal subtotal, CustomerType customer)
    {
        if (customer == CustomerType.Institutional)
        {
            return 0m; // doesn't combine with the institutional discount
        }

        if (subtotal < coupon.MinimumSubtotal)
        {
            return 0m;
        }

        decimal discount = Math.Round(subtotal * coupon.Percent / 100m, 2, MidpointRounding.AwayFromZero);
        if (coupon.MaxDiscount is decimal max && discount > max)
        {
            discount = max;
        }

        return discount;
    }
}
