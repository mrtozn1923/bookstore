using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookstore.Institutional;

internal sealed class InstitutionConfiguration : IEntityTypeConfiguration<Institution>
{
    public void Configure(EntityTypeBuilder<Institution> institution)
    {
        institution.ToTable("Institutions", "institutional");
        institution.Property(i => i.Name).HasMaxLength(200);
        institution.Property(i => i.Code).HasMaxLength(20).IsUnicode(false);
        institution.HasIndex(i => i.Code).IsUnique();
        institution.Property(i => i.Kind).HasConversion<string>().HasMaxLength(20);
        institution.HasMany(i => i.Units).WithOne().HasForeignKey(u => u.InstitutionId).OnDelete(DeleteBehavior.Cascade);
        institution.HasData(InstitutionalSeed.Institutions);
    }
}

internal sealed class InstitutionUnitConfiguration : IEntityTypeConfiguration<InstitutionUnit>
{
    public void Configure(EntityTypeBuilder<InstitutionUnit> unit)
    {
        unit.ToTable("InstitutionUnits", "institutional");
        unit.Property(u => u.Name).HasMaxLength(200);
        unit.HasOne(u => u.ParentUnit).WithMany().HasForeignKey(u => u.ParentUnitId).OnDelete(DeleteBehavior.Restrict);
        unit.HasData(InstitutionalSeed.Units);
    }
}
