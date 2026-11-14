using Bookstore.Data;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Ordering;

public sealed class CouponService(BookstoreContext db)
{
    public async Task<Coupon?> FindValidAsync(string code, decimal subtotal, CancellationToken ct)
    {
        string normalized = code.Trim().ToUpperInvariant();
        Coupon? coupon = await db.Coupons.AsNoTracking().FirstOrDefaultAsync(c => c.Code == normalized, ct);
        if (coupon is null || coupon.ExpiresAtUtc < DateTime.Now || subtotal < coupon.MinSubtotal)
        {
            return null;
        }
        return coupon;
    }

    public async Task<decimal> CalculateDiscountAsync(Coupon coupon, CartPrice price, CancellationToken ct)
    {
        decimal eligible = 0m;
        foreach (PricedLine line in price.Lines)
        {
            if (coupon.CategoryId is null)
            {
                eligible += line.LineTotal;
                continue;
            }
            int categoryId = await db.Books.AsNoTracking()
                .Where(b => b.Id == line.Book.Id)
                .Select(b => b.CategoryId)
                .FirstAsync(ct);
            if (categoryId == coupon.CategoryId)
            {
                eligible += line.LineTotal;
            }
        }
        return CouponRules.CalculateDiscount(coupon, eligible);
    }
}
