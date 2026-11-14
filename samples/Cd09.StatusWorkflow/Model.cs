using Microsoft.EntityFrameworkCore;

namespace Bookstore.Institutional.Workflow;

public enum OrderStatus { Draft = 1, PendingApproval = 2, Approved = 3, Rejected = 4, Cancelled = 5, Shipped = 6, Delivered = 7 }

public class Order
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public string OrderNumber { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public class StatusTransition
{
    public OrderStatus FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    public string Action { get; set; } = "";
    public string RequiredRole { get; set; } = "";
    public bool CommentRequired { get; set; }
}

public class StatusHistory
{
    public long Id { get; set; }
    public int OrderId { get; set; }
    public OrderStatus? FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    public string ChangedBy { get; set; } = "";
    public DateTimeOffset ChangedAt { get; set; }
    public string? Comment { get; set; }
}

public sealed record UserInfo(string Name, string[] Roles);

public class BookstoreContext(DbContextOptions<BookstoreContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<StatusTransition> StatusTransitions => Set<StatusTransition>();
    public DbSet<StatusHistory> StatusHistory => Set<StatusHistory>();

    protected override void ConfigureConventions(ModelConfigurationBuilder c)
        => c.Properties<string>().HaveMaxLength(200);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(e =>
        {
            e.Property(x => x.OrderNumber).HasMaxLength(20);
            e.Property(x => x.TotalAmount).HasPrecision(19, 4);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<StatusTransition>(e =>
        {
            e.HasKey(x => new { x.FromStatus, x.ToStatus });
            e.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(20);
            e.HasData(
                T(OrderStatus.Draft, OrderStatus.PendingApproval, "Submit", "InstitutionBuyer"),
                T(OrderStatus.Draft, OrderStatus.Cancelled, "Cancel", "InstitutionBuyer"),
                T(OrderStatus.PendingApproval, OrderStatus.Approved, "Approve", "InstitutionApprover"),
                T(OrderStatus.PendingApproval, OrderStatus.Rejected, "Reject", "InstitutionApprover", commentRequired: true),
                T(OrderStatus.Rejected, OrderStatus.Draft, "Revise", "InstitutionBuyer"),
                T(OrderStatus.Approved, OrderStatus.Shipped, "Ship", "WarehouseStaff"),
                T(OrderStatus.Approved, OrderStatus.Cancelled, "Cancel", "InstitutionApprover", commentRequired: true),
                T(OrderStatus.Shipped, OrderStatus.Delivered, "Deliver", "WarehouseStaff"));
        });

        modelBuilder.Entity<StatusHistory>(e =>
        {
            e.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(20);
            e.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId);
            // only an allowed transition can be written to history; with FromStatus NULL (first entry) the FK isn't checked
            e.HasOne<StatusTransition>().WithMany()
                .HasForeignKey(x => new { x.FromStatus, x.ToStatus })
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static StatusTransition T(OrderStatus from, OrderStatus to, string action, string role, bool commentRequired = false)
        => new() { FromStatus = from, ToStatus = to, Action = action, RequiredRole = role, CommentRequired = commentRequired };
}

public sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
