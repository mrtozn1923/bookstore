using Bookstore.Institutional.Domain;
using Bookstore.Institutional.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Institutional.ErDiagram;

// Gerçek modelin configuration'ları + iki fiziksel varyant (yalnız bu yazının denemesi için)
public sealed class ErVariantContext(DbContextOptions<ErVariantContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<InstitutionUser> InstitutionUsers => Set<InstitutionUser>();
    public DbSet<InstitutionUnit> InstitutionUnits => Set<InstitutionUnit>();
    public DbSet<BudgetCategory> BudgetCategories => Set<BudgetCategory>();
    public DbSet<UnitBudget> UnitBudgets => Set<UnitBudget>();
    public DbSet<BudgetAdjustment> BudgetAdjustments => Set<BudgetAdjustment>();
    public DbSet<PurchaseApproval> PurchaseApprovals => Set<PurchaseApproval>();
    public DbSet<DiscountAgreement> DiscountAgreements => Set<DiscountAgreement>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.ApplyConfigurationsFromAssembly(typeof(BookstoreContext).Assembly);
        // Composition: ek dosya ayrı tabloda ama kendi kimliği yok (PK = FK)
        b.Entity<Order>().OwnsOne(o => o.Attachment, a => a.ToTable("OrderAttachments"));
        // Derived attribute (/RemainingAmount) -> computed column; C# property'nin setter'ı yok,
        // bu yüzden kolon bir shadow property'ye bağlanır
        b.Entity<UnitBudget>().Property<decimal>("RemainingAmountColumn").HasColumnName("RemainingAmount")
            .HasComputedColumnSql("[AllocatedAmount] - [SpentAmount]").HasPrecision(18, 2);
    }
}

public abstract class OrderBase
{
    public int Id { get; set; }
    public int Quantity { get; set; }
}

public class ClassroomSetOrder : OrderBase
{
    public int ClassCount { get; set; }
}

public class DonationOrder : OrderBase
{
    public string? DonationLetterPath { get; set; }
}

public abstract class HierarchyContext : DbContext
{
    public DbSet<OrderBase> Orders => Set<OrderBase>();

    protected override void OnConfiguring(DbContextOptionsBuilder o) => o.UseSqlServer();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<ClassroomSetOrder>();
        b.Entity<DonationOrder>();
    }
}

// Varsayılan strateji: TPH (tek tablo + Discriminator)
public class TphContext : HierarchyContext;

public class TptContext : HierarchyContext
{
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<OrderBase>().UseTptMappingStrategy();
    }
}
