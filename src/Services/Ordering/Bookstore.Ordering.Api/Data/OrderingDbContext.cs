using Bookstore.Ordering.CatalogCopy;
using Bookstore.Ordering.Checkout;
using Bookstore.Ordering.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookstore.Ordering.Data;

public sealed class OrderingDbContext(DbContextOptions<OrderingDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<CatalogBook> CatalogBooks => Set<CatalogBook>();
    public DbSet<CheckoutSaga> CheckoutSagas => Set<CheckoutSaga>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(order =>
        {
            // Guid v7 is time-ordered, but SQL Server sorts uniqueidentifier by its last bytes first:
            // a clustered index on it would still fragment. Cluster on the placement time instead.
            order.HasKey(o => o.Id).IsClustered(false);
            order.HasIndex(o => o.PlacedAtUtc).IsClustered();
            order.Property(o => o.Id).HasConversion(id => id.Value, value => new OrderId(value));
            order.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
            order.Property(o => o.CancellationReason).HasMaxLength(200);
            order.Property(o => o.PaymentReference).HasMaxLength(100);
            order.ComplexProperty(o => o.Buyer, buyer =>
            {
                buyer.Property(b => b.Name).HasMaxLength(200);
                buyer.Property(b => b.Email).HasMaxLength(256);
            });
            order.ComplexProperty(o => o.Total, MapMoney);
            order.Property<byte[]>("RowVersion").IsRowVersion();
            order.HasMany(o => o.Items).WithOne().HasForeignKey("OrderId").IsRequired().OnDelete(DeleteBehavior.Cascade);
            order.Navigation(o => o.Items).HasField("_items").AutoInclude();
            order.Ignore(o => o.DomainEvents);
        });

        modelBuilder.Entity<OrderItem>(item =>
        {
            item.ToTable("OrderItems");
            item.Property<int>("Id");
            item.HasKey("Id");
            item.Property(i => i.Isbn).HasMaxLength(13).IsUnicode(false);
            item.Property(i => i.Title).HasMaxLength(200);
            item.ComplexProperty(i => i.UnitPrice, MapMoney);
            item.ComplexProperty(i => i.LineTotal, MapMoney);
        });

        // Wolverine finds the saga's storage through this mapping and increments Version on every save:
        // two messages for the same checkout handled at the same time cannot overwrite each other.
        modelBuilder.Entity<CheckoutSaga>(saga =>
        {
            saga.HasKey(s => s.Id);
            saga.Property(s => s.Step).HasConversion<string>().HasMaxLength(30);
            saga.Property(s => s.Total).HasPrecision(12, 2);
            saga.Property(s => s.Currency).HasMaxLength(3).IsUnicode(false);
            saga.Property(s => s.CustomerEmail).HasMaxLength(256);
            saga.Property(s => s.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<CatalogBook>(book =>
        {
            book.HasKey(b => b.Isbn);
            book.Property(b => b.Isbn).HasMaxLength(13).IsUnicode(false);
            book.Property(b => b.Title).HasMaxLength(200);
            book.Property(b => b.Price).HasPrecision(10, 2);
            book.Property(b => b.Currency).HasMaxLength(3).IsUnicode(false);
        });
    }

    private static void MapMoney(ComplexPropertyBuilder<Money> money)
    {
        money.Property(m => m.Amount).HasPrecision(12, 2);
        money.Property(m => m.Currency).HasMaxLength(3).IsUnicode(false);
    }
}
