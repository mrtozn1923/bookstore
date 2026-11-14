using Bookstore.Institutional.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookstore.Institutional.Infrastructure.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        // Sipariş numaraları 42'den başlar (serinin örnek verisi)
        b.Property(o => o.Id).UseIdentityColumn(42);
        // «dataType» Money: kimliği yok, tabloda iki kolon olur
        b.ComplexProperty(o => o.Total, m =>
        {
            m.Property(x => x.Amount).HasColumnName("TotalAmount").HasPrecision(18, 2);
            m.Property(x => x.Currency).HasColumnName("Currency").HasMaxLength(3);
        });
        b.Property(o => o.Status).HasConversion<string>().HasMaxLength(32);
        b.Property(o => o.Note).HasMaxLength(500);
        b.Property(o => o.ShipmentPreference).HasConversion<string>().HasMaxLength(20);
        // composition [0..1]: owned type, aynı tabloda
        b.OwnsOne(o => o.Attachment, a =>
        {
            a.Property(x => x.FileName).HasMaxLength(260);
            a.Property(x => x.ContentType).HasMaxLength(100);
            a.Property(x => x.StoragePath).HasMaxLength(400);
        });
        // composition [0..*]: parent silinince onaylar da silinir
        b.HasMany(o => o.Approvals).WithOne(a => a.Order)
            .HasForeignKey(a => a.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(o => o.Approvals).HasField("approvals");
        // association: sipariş kullanıcıya ve bütçe kalemine bağlı, onların parçası değil
        b.HasOne(o => o.PlacedBy).WithMany().HasForeignKey(o => o.PlacedById).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(o => o.BudgetCategory).WithMany().HasForeignKey(o => o.BudgetCategoryId).OnDelete(DeleteBehavior.Restrict);
        // derived attribute: veritabanında kolon yok
        b.Ignore(o => o.RequiresPurchasingApproval);
    }
}
