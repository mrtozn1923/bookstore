using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Bookstore.Institutional.PublicIds;

public readonly record struct OrderPublicId(Guid Value)
{
    public static OrderPublicId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}

public readonly record struct InstitutionUserPublicId(Guid Value)
{
    public static InstitutionUserPublicId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}

public class Order
{
    public long Id { get; private set; }                 // internal only: joins, FKs, the clustered index
    public OrderPublicId PublicId { get; private set; } = OrderPublicId.New();
    public int InstitutionId { get; set; }
    public decimal TotalAmount { get; set; }
}

public class AuditEntry
{
    public Guid Id { get; set; }                         // EF Core generates it on the client
    public string Action { get; set; } = "";
}

public class BookstoreContext(DbContextOptions<BookstoreContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Order>(e =>
        {
            e.Property(o => o.TotalAmount).HasPrecision(19, 4);
            e.Property(o => o.PublicId)
                .HasConversion(v => v.Value, v => new OrderPublicId(v))
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
            e.HasIndex(o => o.PublicId).IsUnique();
        });
        b.Entity<AuditEntry>().Property(a => a.Action).HasMaxLength(100);
    }
}

public static class OrderQueries
{
    public static IQueryable<Order> FindQuery(BookstoreContext db, int institutionId, OrderPublicId publicId)
        => db.Orders.Where(o => o.PublicId == publicId && o.InstitutionId == institutionId);

    // An unguessable id is not authorization: the institution condition is in every query
    public static Task<Order?> FindAsync(BookstoreContext db, int institutionId, OrderPublicId publicId)
        => FindQuery(db, institutionId, publicId).AsNoTracking().SingleOrDefaultAsync();
}
