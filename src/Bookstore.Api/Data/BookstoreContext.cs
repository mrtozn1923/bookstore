using Bookstore.Api.FileStorage;
using Bookstore.Api.Institutional;
using Bookstore.Notifications.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Api.Data;

// Bookstore runs on SQL Server; this series uses SQLite so the sample needs no database server
public sealed class BookstoreContext(DbContextOptions<BookstoreContext> options) : DbContext(options)
{
    public DbSet<PurchaseApproval> PurchaseApprovals => Set<PurchaseApproval>();
    public DbSet<StoredFileRecord> StoredFiles => Set<StoredFileRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyEmailOutbox();
        modelBuilder.Entity<StoredFileRecord>(e =>
        {
            e.ToTable("StoredFiles");
            e.HasKey(x => x.Path);
            e.Property(x => x.Path).HasMaxLength(512);
            e.HasIndex(x => new { x.Purpose, x.CreatedAtUtc });
        });
        modelBuilder.Entity<PurchaseApproval>(e =>
        {
            e.Property(x => x.Total).HasConversion<double>();
            e.HasData(
                new PurchaseApproval { Id = 1, OrderId = 2101, InstitutionName = "Hilltop Primary School", ApproverName = "Nevin", ApproverEmail = "nevin@example.com", ApproverCulture = "tr-TR", RequestedBy = "Emre", UnitName = "Science Department", Total = 9_120.00m, RequestedAtUtc = new DateTime(2026, 11, 10, 9, 30, 0, DateTimeKind.Utc) },
                new PurchaseApproval { Id = 2, OrderId = 2107, InstitutionName = "Hilltop Primary School", ApproverName = "Nevin", ApproverEmail = "nevin@example.com", ApproverCulture = "tr-TR", RequestedBy = "Ayse", UnitName = "Library", Total = 4_560.00m, RequestedAtUtc = new DateTime(2026, 11, 11, 14, 5, 0, DateTimeKind.Utc), Note = "<b>Urgent</b> for the reading week <script>alert('x')</script>" },
                new PurchaseApproval { Id = 3, OrderId = 2112, InstitutionName = "Riverside City Library", ApproverName = "Grace", ApproverEmail = "grace@example.com", ApproverCulture = "en-US", RequestedBy = "Linus", UnitName = "Children's Section", Total = 3_040.00m, RequestedAtUtc = new DateTime(2026, 11, 12, 8, 0, 0, DateTimeKind.Utc) });
        });
    }
}
