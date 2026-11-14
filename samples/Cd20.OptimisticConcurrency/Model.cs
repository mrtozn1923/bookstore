using Microsoft.EntityFrameworkCore;

namespace Bookstore.Api.Concurrency;

public class InstitutionUser
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public decimal? SpendingLimit { get; set; }
    public int InstitutionUnitId { get; set; }
    public byte[] Version { get; set; } = [];
}

public record UpdateInstitutionUserRequest(string FullName, string Phone, decimal? SpendingLimit, int InstitutionUnitId);

public record InstitutionUserDto(int Id, string FullName, string Phone, decimal? SpendingLimit, int InstitutionUnitId)
{
    public static InstitutionUserDto From(InstitutionUser u) => new(u.Id, u.FullName, u.Phone, u.SpendingLimit, u.InstitutionUnitId);
    public UpdateInstitutionUserRequest ToRequest() => new(FullName, Phone, SpendingLimit, InstitutionUnitId);
}

public class BookstoreContext(DbContextOptions<BookstoreContext> options) : DbContext(options)
{
    public DbSet<InstitutionUser> InstitutionUsers => Set<InstitutionUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InstitutionUser>().Property(u => u.SpendingLimit).HasPrecision(18, 2);
        // SQL Server rowversion column
        modelBuilder.Entity<InstitutionUser>().Property(u => u.Version).IsRowVersion();
    }
}
