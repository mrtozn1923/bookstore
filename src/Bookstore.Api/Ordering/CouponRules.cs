using System.Diagnostics;

namespace Bookstore.Ordering;

public static class CouponRules
{
    public static decimal CalculateDiscount(Coupon coupon, decimal eligibleSubtotal) => coupon.Kind switch
    {
        CouponKind.Percent => Math.Round(eligibleSubtotal * coupon.Value / 100m, 2, MidpointRounding.AwayFromZero),
        CouponKind.Amount => coupon.Value,
        _ => throw new UnreachableException()
    };
}
