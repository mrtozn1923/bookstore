using Bookstore.Catalog;
using Bookstore.Inventory;
using Bookstore.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Bookstore.Data;

// One context for the reference app; each module maps to its own SQL schema (catalog, ordering, inventory).
public sealed class BookstoreContext(DbContextOptions<BookstoreContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookstoreContext).Assembly);

    // All times are stored in UTC; values read back are marked as UTC too.
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        if (Database.IsSqlite())
        {
            // SQLite cannot compare or sort decimal columns; local development stores them as REAL.
            configurationBuilder.Properties<decimal>().HaveConversion<double>();
        }
    }

    private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
}
