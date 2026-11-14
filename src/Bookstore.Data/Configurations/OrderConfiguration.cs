using Bookstore.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookstore.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.Property(x => x.OrderNumber)
            .HasDefaultValueSql("NEXT VALUE FOR ordering.OrderNumbers");
        b.HasIndex(x => x.OrderNumber).IsUnique();

        b.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        b.Property(x => x.ShippingFee)
            .HasDefaultValue(4.99m)
            .ValueGeneratedNever();
        b.Property(x => x.TotalAmount)
            .HasComputedColumnSql("[Subtotal] - [DiscountAmount] + [ShippingFee]", stored: true);
    }
}
