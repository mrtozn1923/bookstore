using Microsoft.EntityFrameworkCore;

namespace Bookstore.Api.Data;

public sealed class BookstoreDbContext(DbContextOptions<BookstoreDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(book =>
        {
            book.Property(b => b.Isbn).HasMaxLength(17);
            book.HasIndex(b => b.Isbn).IsUnique();
            book.Property(b => b.Title).HasMaxLength(200);
            book.Property(b => b.Price).HasPrecision(10, 2);
        });

        modelBuilder.Entity<Coupon>(coupon =>
        {
            coupon.Property(c => c.Code).HasMaxLength(32);
            coupon.HasIndex(c => c.Code).IsUnique();
            coupon.ToTable(t => t.HasCheckConstraint("CK_Coupons_PercentOff", "[PercentOff] BETWEEN 1 AND 100"));
        });

        modelBuilder.Entity<Customer>(customer =>
        {
            customer.Property(c => c.Email).HasMaxLength(256);
            customer.HasIndex(c => c.Email).IsUnique();
            customer.Property(c => c.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Order>(order =>
        {
            order.Property(o => o.Reference).HasMaxLength(20);
            order.HasIndex(o => o.Reference).IsUnique();
            order.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
            order.Property(o => o.Total).HasPrecision(10, 2);
            order.Property(o => o.PaymentId).HasMaxLength(64);
            order.Property(o => o.TrackingNumber).HasMaxLength(64);
            order.HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId);
        });

        modelBuilder.Entity<OrderItem>(item =>
        {
            item.Property(i => i.UnitPrice).HasPrecision(10, 2);
            item.HasOne<Book>().WithMany().HasForeignKey(i => i.BookId);
        });
    }
}
