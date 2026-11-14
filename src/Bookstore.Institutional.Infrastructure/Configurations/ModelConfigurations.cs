using Bookstore.Institutional.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookstore.Institutional.Infrastructure.Configurations;

internal sealed class PurchaseApprovalConfiguration : IEntityTypeConfiguration<PurchaseApproval>
{
    public void Configure(EntityTypeBuilder<PurchaseApproval> b)
    {
        b.HasOne(a => a.Approver).WithMany().HasForeignKey(a => a.ApproverId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(a => new { a.OrderId, a.Step }).IsUnique();
        b.Property(a => a.Step).HasConversion<string>().HasMaxLength(32);
        b.Property(a => a.Decision).HasConversion<string>().HasMaxLength(16);
        b.Property(a => a.Comment).HasMaxLength(500);
    }
}

internal sealed class InstitutionUserConfiguration : IEntityTypeConfiguration<InstitutionUser>
{
    public void Configure(EntityTypeBuilder<InstitutionUser> b)
    {
        b.HasIndex(u => u.StaffNumber).IsUnique();
        b.Property(u => u.StaffNumber).HasMaxLength(16);
        b.Property(u => u.FullName).HasMaxLength(200);
        b.Property(u => u.Email).HasMaxLength(256);
        b.HasOne(u => u.Approver).WithMany().HasForeignKey(u => u.ApproverId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(u => u.InstitutionUnit).WithMany().HasForeignKey(u => u.InstitutionUnitId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class InstitutionUnitConfiguration : IEntityTypeConfiguration<InstitutionUnit>
{
    public void Configure(EntityTypeBuilder<InstitutionUnit> b)
    {
        b.Property(u => u.Name).HasMaxLength(200);
        b.HasOne(u => u.PurchasingManager).WithMany().HasForeignKey(u => u.PurchasingManagerId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class BudgetCategoryConfiguration : IEntityTypeConfiguration<BudgetCategory>
{
    public void Configure(EntityTypeBuilder<BudgetCategory> b)
    {
        b.HasIndex(c => c.Code).IsUnique();
        b.Property(c => c.Code).HasMaxLength(32);
        b.Property(c => c.Name).HasMaxLength(100);
    }
}

internal sealed class UnitBudgetConfiguration : IEntityTypeConfiguration<UnitBudget>
{
    public void Configure(EntityTypeBuilder<UnitBudget> b)
    {
        // qualified association: birim + yıl + kalem için en fazla bir bütçe
        b.HasIndex(x => new { x.InstitutionUnitId, x.Year, x.BudgetCategoryId }).IsUnique();
        b.Property(x => x.AllocatedAmount).HasPrecision(18, 2);
        b.Property(x => x.SpentAmount).HasPrecision(18, 2);
        b.Ignore(x => x.RemainingAmount);
    }
}

internal sealed class BudgetAdjustmentConfiguration : IEntityTypeConfiguration<BudgetAdjustment>
{
    public void Configure(EntityTypeBuilder<BudgetAdjustment> b)
    {
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Reason).HasMaxLength(500);
    }
}

internal sealed class DiscountAgreementConfiguration : IEntityTypeConfiguration<DiscountAgreement>
{
    public void Configure(EntityTypeBuilder<DiscountAgreement> b) => b.Property(x => x.DiscountRate).HasPrecision(5, 4);
}
