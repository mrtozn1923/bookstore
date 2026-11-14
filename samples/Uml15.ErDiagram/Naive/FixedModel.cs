using Microsoft.EntityFrameworkCore;

namespace Bookstore.Institutional.Fixed;

// Döngüdeki bir uç optional: birim, satın alma müdürü atanmadan da var olabilir
public class InstitutionUnit
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int? PurchasingManagerId { get; set; }
    public InstitutionUser? PurchasingManager { get; set; }
}

public class InstitutionUser
{
    public int Id { get; set; }
    public required string StaffNumber { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public int InstitutionUnitId { get; set; }
    public InstitutionUnit InstitutionUnit { get; set; } = null!;
}

public sealed class FixedContext(string connectionString) : DbContext
{
    public DbSet<InstitutionUnit> InstitutionUnits => Set<InstitutionUnit>();
    public DbSet<InstitutionUser> InstitutionUsers => Set<InstitutionUser>();

    protected override void OnConfiguring(DbContextOptionsBuilder o) => o.UseSqlServer(connectionString);

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<InstitutionUnit>().HasOne(u => u.PurchasingManager).WithMany()
            .HasForeignKey(u => u.PurchasingManagerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<InstitutionUser>().HasOne(u => u.InstitutionUnit).WithMany()
            .HasForeignKey(u => u.InstitutionUnitId).OnDelete(DeleteBehavior.Restrict);
    }
}
