using Bookstore.Institutional.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Institutional.Infrastructure;

public sealed class BookstoreContext(DbContextOptions<BookstoreContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<InstitutionUser> InstitutionUsers => Set<InstitutionUser>();
    public DbSet<InstitutionUnit> InstitutionUnits => Set<InstitutionUnit>();
    public DbSet<BudgetCategory> BudgetCategories => Set<BudgetCategory>();
    public DbSet<UnitBudget> UnitBudgets => Set<UnitBudget>();
    public DbSet<BudgetAdjustment> BudgetAdjustments => Set<BudgetAdjustment>();
    public DbSet<PurchaseApproval> PurchaseApprovals => Set<PurchaseApproval>();
    public DbSet<DiscountAgreement> DiscountAgreements => Set<DiscountAgreement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookstoreContext).Assembly);
}
