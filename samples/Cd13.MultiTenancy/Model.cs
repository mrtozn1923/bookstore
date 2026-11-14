using Microsoft.EntityFrameworkCore;

namespace Bookstore.Institutional.MultiTenancy;

// Every row that belongs to one institution (tenant) carries its InstitutionId
public interface ITenantOwned
{
    int InstitutionId { get; set; }
}

public class InstitutionUser : ITenantOwned
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
}

public enum ApprovalStatus { Pending = 1, Approved = 2, Rejected = 3 }

public class PurchaseApproval : ITenantOwned
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public int OrderId { get; set; }
    public int RequestedById { get; set; }
    public int ApproverId { get; set; }
    public ApprovalStatus Status { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
}

public class Invoice : ITenantOwned
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public decimal TotalAmount { get; set; }
}

public class BookstoreContext(DbContextOptions<BookstoreContext> options, ITenantContext tenant) : DbContext(options)
{
    public const string TenantFilter = "Tenant";

    public DbSet<InstitutionUser> InstitutionUsers => Set<InstitutionUser>();
    public DbSet<PurchaseApproval> PurchaseApprovals => Set<PurchaseApproval>();
    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InstitutionUser>(b =>
        {
            b.Property(u => u.Email).HasMaxLength(200);
            b.HasIndex(u => new { u.InstitutionId, u.Email }).IsUnique();
            b.HasQueryFilter(TenantFilter, u => u.InstitutionId == tenant.TenantId);
        });
        modelBuilder.Entity<PurchaseApproval>()
            .HasQueryFilter(TenantFilter, a => a.InstitutionId == tenant.TenantId);
        modelBuilder.Entity<Invoice>().Property(i => i.TotalAmount).HasPrecision(19, 4);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<ITenantOwned>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.InstitutionId = tenant.TenantId;
            else if (entry.State == EntityState.Modified && entry.Property(e => e.InstitutionId).IsModified)
                throw new InvalidOperationException("InstitutionId cannot be changed.");
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}

// The first version: every query writes its own tenant filter
public class NaiveContext(DbContextOptions<NaiveContext> options) : DbContext(options)
{
    public DbSet<InstitutionUser> InstitutionUsers => Set<InstitutionUser>();
}

public class InstitutionUserReports(NaiveContext db)
{
    public Task<List<string>> ListAsync(int institutionId) =>
        db.InstitutionUsers.Where(u => u.InstitutionId == institutionId).Select(u => u.Email).ToListAsync();

    public Task<int> CountAsync(int institutionId) =>
        db.InstitutionUsers.CountAsync();
}
