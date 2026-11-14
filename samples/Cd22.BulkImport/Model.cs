using Microsoft.EntityFrameworkCore;

namespace Bookstore.Institutional.Import;

public class InstitutionUnit
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}

public class InstitutionUser
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public string StaffNumber { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public int InstitutionUnitId { get; set; }
    public DateOnly JoinedOn { get; set; }
    public decimal? SpendingLimit { get; set; }
}

public enum ImportStatus { Queued, Loading, Validating, Validated, Committed, Failed }

public enum CommitMode { AllOrNothing, ValidOnly }

public class InstitutionUserImport
{
    public Guid Id { get; set; }
    public int InstitutionId { get; set; }
    public string FileName { get; set; } = "";
    public byte[] FileHash { get; set; } = [];
    public ImportStatus Status { get; set; }
    public int TotalRows { get; set; }
    public int InvalidRows { get; set; }
    public int CommittedRows { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed record ImportStatusDto(Guid Id, string FileName, string Status, int TotalRows, int InvalidRows,
    int CommittedRows, string? FailureReason, bool Duplicate)
{
    public static ImportStatusDto From(InstitutionUserImport i, bool duplicate = false) =>
        new(i.Id, i.FileName, i.Status.ToString(), i.TotalRows, i.InvalidRows, i.CommittedRows, i.FailureReason, duplicate);
}

public class BookstoreContext(DbContextOptions<BookstoreContext> options) : DbContext(options)
{
    public DbSet<InstitutionUnit> InstitutionUnits => Set<InstitutionUnit>();
    public DbSet<InstitutionUser> InstitutionUsers => Set<InstitutionUser>();
    public DbSet<InstitutionUserImport> InstitutionUserImports => Set<InstitutionUserImport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InstitutionUnit>(e =>
        {
            e.Property(u => u.Code).HasMaxLength(10);
            e.HasIndex(u => new { u.InstitutionId, u.Code }).IsUnique();
        });
        modelBuilder.Entity<InstitutionUser>(e =>
        {
            e.Property(u => u.StaffNumber).HasMaxLength(5).IsFixedLength();
            e.Property(u => u.Email).HasMaxLength(200);
            e.Property(u => u.SpendingLimit).HasPrecision(19, 4);
            e.HasIndex(u => new { u.InstitutionId, u.Email }).IsUnique();
        });
        modelBuilder.Entity<InstitutionUserImport>(e =>
        {
            e.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(i => i.FileHash).HasMaxLength(32).IsFixedLength();
            e.HasIndex(i => new { i.InstitutionId, i.FileHash }).IsUnique();
        });
    }
}
