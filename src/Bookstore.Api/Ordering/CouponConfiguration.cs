using Bookstore.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookstore.Ordering;

internal sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> coupon)
    {
        coupon.ToTable("Coupons", "ordering");
        coupon.Property(c => c.Code).HasMaxLength(32).IsUnicode(false);
        coupon.HasIndex(c => c.Code).IsUnique();
        coupon.Property(c => c.Kind).HasConversion<string>().HasMaxLength(10);
        coupon.Property(c => c.Value).HasPrecision(10, 2);
        coupon.Property(c => c.MinSubtotal).HasPrecision(12, 2);
        coupon.Property(c => c.Description).HasMaxLength(500);
        coupon.HasOne<Category>().WithMany().HasForeignKey(c => c.CategoryId).OnDelete(DeleteBehavior.Restrict);

        DateTime endOfYear = new(2026, 12, 31, 20, 59, 59, DateTimeKind.Utc);
        coupon.HasData(
            new Coupon { Id = 1, Code = "BOOK10", Kind = CouponKind.Percent, Value = 10m, ExpiresAtUtc = endOfYear,
                Description = "<strong>10%</strong> off your whole cart" },
            new Coupon { Id = 2, Code = "SAVE50", Kind = CouponKind.Amount, Value = 50m, MinSubtotal = 300m, ExpiresAtUtc = endOfYear,
                Description = "<strong>50 TRY</strong> off orders over 300 TRY" },
            new Coupon { Id = 3, Code = "CLASSIC15", Kind = CouponKind.Percent, Value = 15m, CategoryId = 1, ExpiresAtUtc = endOfYear,
                Description = "<strong>15%</strong> off classics" },
            new Coupon { Id = 4, Code = "WELCOME25", Kind = CouponKind.Amount, Value = 25m, ExpiresAtUtc = endOfYear,
                Description = "<strong>25 TRY</strong> off your first order" });
    }
}
