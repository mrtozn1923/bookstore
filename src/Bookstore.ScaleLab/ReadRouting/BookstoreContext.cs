using Microsoft.EntityFrameworkCore;

namespace Bookstore.ScaleLab.ReadRouting;

// writes (and reads that must be current) go through this context: always the primary
public sealed class BookstoreContext(DbContextOptions<BookstoreContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => OrderMapping.Configure(modelBuilder);
}

// reads that may be slightly stale: a replica when possible, never tracked, never saved
public sealed class BookstoreReadContext(DbContextOptions<BookstoreReadContext> options) : DbContext(options)
{
    public IQueryable<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => OrderMapping.Configure(modelBuilder);

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("BookstoreReadContext is read-only.");
}
