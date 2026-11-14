using Microsoft.EntityFrameworkCore;

namespace Bookstore.Institutional.SoftDelete;

public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
    Guid? DeletionId { get; set; }
}

public class InstitutionUser : ISoftDeletable
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public int InstitutionUnitId { get; set; }
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? Phone { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public Guid? DeletionId { get; set; }
    public List<PurchaseApproval> PurchaseApprovals { get; set; } = [];
}

public enum ApprovalStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}

public class PurchaseApproval : ISoftDeletable
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public int OrderId { get; set; }
    public int RequestedById { get; set; }   // InstitutionUser who placed the order
    public ApprovalStatus Status { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public Guid? DeletionId { get; set; }
}

public class BookstoreContext(DbContextOptions<BookstoreContext> options, int institutionId) : DbContext(options)
{
    public const string SoftDeleteFilter = "SoftDelete";
    public const string TenantFilter = "Tenant";

    public DbSet<InstitutionUser> InstitutionUsers => Set<InstitutionUser>();
    public DbSet<PurchaseApproval> PurchaseApprovals => Set<PurchaseApproval>();

    protected override void ConfigureConventions(ModelConfigurationBuilder c)
        => c.Properties<string>().HaveMaxLength(200);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InstitutionUser>(e =>
        {
            e.HasIndex(x => new { x.InstitutionId, x.Email })
                .IsUnique()
                .HasDatabaseName("UX_InstitutionUsers_Email_Active")
                .HasFilter("[DeletedAt] IS NULL");
            e.HasIndex(x => x.DeletionId);
            e.Property(x => x.DeletedBy).HasMaxLength(100);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.HasQueryFilter(SoftDeleteFilter, x => x.DeletedAt == null);
            e.HasQueryFilter(TenantFilter, x => x.InstitutionId == institutionId);
        });

        modelBuilder.Entity<PurchaseApproval>(a =>
        {
            a.HasOne<InstitutionUser>().WithMany(u => u.PurchaseApprovals)
                .HasForeignKey(x => x.RequestedById).OnDelete(DeleteBehavior.Restrict);
            a.HasIndex(x => x.DeletionId);
            a.Property(x => x.DeletedBy).HasMaxLength(100);
            a.HasQueryFilter(SoftDeleteFilter, x => x.DeletedAt == null);
            a.HasQueryFilter(TenantFilter, x => x.InstitutionId == institutionId);
        });
    }
}
