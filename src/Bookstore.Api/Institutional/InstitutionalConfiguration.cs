using Bookstore.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookstore.Institutional;

internal sealed class InstitutionConfiguration : IEntityTypeConfiguration<Institution>
{
    public void Configure(EntityTypeBuilder<Institution> institution)
    {
        institution.ToTable("Institutions", "institutional");
        institution.Property(i => i.Name).HasMaxLength(200);
        institution.Property(i => i.BillingEmail).HasMaxLength(256);
        institution.HasData(
            new Institution { Id = 1, Name = "Lakeside School", BillingEmail = "purchasing@lakeside-school.example.com" },
            new Institution { Id = 2, Name = "Hillcrest University", BillingEmail = "library@hillcrest.example.edu" });
    }
}

internal sealed class InstitutionUnitConfiguration : IEntityTypeConfiguration<InstitutionUnit>
{
    public void Configure(EntityTypeBuilder<InstitutionUnit> unit)
    {
        unit.ToTable("InstitutionUnits", "institutional");
        unit.Property(u => u.Name).HasMaxLength(200);
        unit.Property(u => u.ApprovalLimit).HasPrecision(12, 2);
        unit.HasOne<Institution>().WithMany().HasForeignKey(u => u.InstitutionId);
        unit.HasData(
            new InstitutionUnit { Id = 1, InstitutionId = 1, Name = "Library", ApprovalLimit = 5000m },
            new InstitutionUnit { Id = 2, InstitutionId = 2, Name = "Central Library", ApprovalLimit = 20000m });
    }
}

internal sealed class PurchaseRequestConfiguration : IEntityTypeConfiguration<PurchaseRequest>
{
    public void Configure(EntityTypeBuilder<PurchaseRequest> request)
    {
        request.ToTable("PurchaseRequests", "institutional");
        request.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        request.Property(r => r.RequestedBy).HasMaxLength(100);
        request.Property(r => r.ApprovedBy).HasMaxLength(100);
        request.Property(r => r.ApprovedLinesHash).HasMaxLength(64).IsUnicode(false);
        request.Property(r => r.Total).HasPrecision(12, 2);
        request.HasOne<InstitutionUnit>().WithMany().HasForeignKey(r => r.UnitId);
        request.HasMany(r => r.Lines).WithOne().HasForeignKey(l => l.PurchaseRequestId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PurchaseRequestLineConfiguration : IEntityTypeConfiguration<PurchaseRequestLine>
{
    public void Configure(EntityTypeBuilder<PurchaseRequestLine> line)
    {
        line.ToTable("PurchaseRequestLines", "institutional");
        line.Property(l => l.UnitPrice).HasPrecision(10, 2);
        line.HasOne<Book>().WithMany().HasForeignKey(l => l.BookId).OnDelete(DeleteBehavior.Restrict);
    }
}
