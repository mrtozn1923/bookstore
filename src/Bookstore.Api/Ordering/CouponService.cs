using Bookstore.Data;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Ordering;

public sealed class CouponService(BookstoreContext db, TimeProvider clock)
{
    public async Task<Coupon?> FindValidAsync(string code, decimal subtotal, CancellationToken ct)
    {
        string normalized = code.Trim().ToUpperInvariant();
        Coupon? coupon = await db.Coupons.AsNoTracking().FirstOrDefaultAsync(c => c.Code == normalized, ct);
        if (coupon is null || coupon.ExpiresAtUtc < clock.GetUtcNow().UtcDateTime || subtotal < coupon.MinSubtotal)
        {
            return null;
        }
        return coupon;
    }

    // The priced lines already carry each book's category; no extra query per line.
    public static decimal CalculateDiscount(Coupon coupon, CartPrice price)
    {
        decimal eligible = price.Lines
            .Where(l => coupon.CategoryId is null || l.Book.CategoryId == coupon.CategoryId)
            .Sum(l => l.LineTotal);
        return CouponRules.CalculateDiscount(coupon, eligible);
    }
}
