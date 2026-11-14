using Microsoft.EntityFrameworkCore;

namespace Bookstore.Institutional.Audit;

public interface IAudited;

[AttributeUsage(AttributeTargets.Property)]
public sealed class PersonalDataAttribute : Attribute;

public class Invoice : IAudited
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public DateOnly DueDate { get; set; }
}

public class InstitutionUser : IAudited
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public string FullName { get; set; } = "";
    [PersonalData]
    public string? Phone { get; set; }
}

public class AuditContext
{
    public string UserName { get; set; } = "system";
    public string? CorrelationId { get; set; }
    public string? Reason { get; set; }
}

public class AuditEntry
{
    public long Id { get; set; }
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string Action { get; set; } = "";
    public string Changes { get; set; } = "";
    public string UserName { get; set; } = "";
    public string? CorrelationId { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
}

public sealed record PropertyChange(string Property, object? Old, object? New);

public class BookstoreContext(DbContextOptions<BookstoreContext> options) : DbContext(options)
{
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InstitutionUser> InstitutionUsers => Set<InstitutionUser>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void ConfigureConventions(ModelConfigurationBuilder c)
        => c.Properties<string>().HaveMaxLength(200);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invoice>(e =>
        {
            e.Property(x => x.InvoiceNumber).HasMaxLength(16);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.HasIndex(x => x.InvoiceNumber).IsUnique();
        });
        modelBuilder.Entity<InstitutionUser>().Property(x => x.Phone).HasMaxLength(20);

        modelBuilder.Entity<AuditEntry>(e =>
        {
            e.Property(x => x.EntityType).HasMaxLength(100);
            e.Property(x => x.EntityId).HasMaxLength(100);
            e.Property(x => x.Action).HasMaxLength(20);
            e.Property(x => x.Changes).HasMaxLength(-1);
            e.Property(x => x.UserName).HasMaxLength(100);
            e.Property(x => x.CorrelationId).HasMaxLength(64);
            e.Property(x => x.Reason).HasMaxLength(500);
            e.HasIndex(x => new { x.EntityType, x.EntityId, x.ChangedAt });
        });
    }
}

public sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
