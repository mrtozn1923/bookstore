using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Bookstore.Institutional.Hierarchy;

public class InstitutionUnit
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public int? ParentId { get; set; }
    public string Name { get; set; } = "";
    public HierarchyId Node { get; set; } = HierarchyId.GetRoot();
}

public class InstitutionUser
{
    public int Id { get; set; }
    public int InstitutionUnitId { get; set; }
    public InstitutionUnit InstitutionUnit { get; set; } = null!;
    public string FullName { get; set; } = "";
}

public sealed class QueryCounter : DbCommandInterceptor
{
    public int Count { get; set; }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken ct = default)
    {
        Count++;
        return base.ReaderExecutingAsync(command, eventData, result, ct);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken ct = default)
    {
        Count++;
        return base.ScalarExecutingAsync(command, eventData, result, ct);
    }
}

public class BookstoreContext(string connectionString, QueryCounter counter) : DbContext
{
    public DbSet<InstitutionUnit> InstitutionUnits => Set<InstitutionUnit>();
    public DbSet<InstitutionUser> InstitutionUsers => Set<InstitutionUser>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlServer(connectionString, sql => sql.UseHierarchyId()).AddInterceptors(counter);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InstitutionUnit>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Name).HasMaxLength(100);
            e.HasOne<InstitutionUnit>().WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.ParentId);
            e.HasIndex(x => x.Node).IsUnique();
        });
        modelBuilder.Entity<InstitutionUser>().Property(x => x.FullName).HasMaxLength(100);
    }
}
