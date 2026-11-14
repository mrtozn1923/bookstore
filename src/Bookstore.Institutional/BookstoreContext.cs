using Microsoft.EntityFrameworkCore;

namespace Bookstore.Institutional;

public class BookstoreContext(DbContextOptions<BookstoreContext> options) : DbContext(options)
{
    public DbSet<Institution> Institutions => Set<Institution>();
    public DbSet<InstitutionUnit> InstitutionUnits => Set<InstitutionUnit>();
    public DbSet<InstitutionUser> InstitutionUsers => Set<InstitutionUser>();
    public DbSet<DiscountAgreement> DiscountAgreements => Set<DiscountAgreement>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<PurchaseApproval> PurchaseApprovals => Set<PurchaseApproval>();
    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void ConfigureConventions(ModelConfigurationBuilder c)
        => c.Properties<string>().HaveMaxLength(200);

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<InstitutionUnit>().HasOne(u => u.Parent).WithMany()
            .HasForeignKey(u => u.ParentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<InstitutionUser>().HasOne<InstitutionUnit>().WithMany()
            .HasForeignKey(u => u.InstitutionUnitId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<InstitutionUser>().HasOne<InstitutionUser>().WithMany()
            .HasForeignKey(u => u.ApproverId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<InstitutionUser>().Property(u => u.SpendingLimit).HasPrecision(19, 4);
        b.Entity<InstitutionUser>().HasIndex(u => u.Email).IsUnique().HasFilter("[IsDeleted] = 0");

        b.Entity<DiscountAgreement>().Property(a => a.DiscountRate).HasPrecision(5, 4);
        b.Entity<Order>().Property(o => o.TotalAmount).HasPrecision(19, 4);
        b.Entity<Order>().Property(o => o.Currency).HasMaxLength(3).IsFixedLength();
        b.Entity<Invoice>().Property(i => i.InvoiceNumber).HasMaxLength(16);
        b.Entity<Invoice>().HasIndex(i => i.InvoiceNumber).IsUnique();
        // the remaining foreign keys (Institution, Order, PlacedBy, Approver) follow the same pattern
    }
}
