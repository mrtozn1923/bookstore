using Bookstore.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Data;

public class BookstoreContext(DbContextOptions<BookstoreContext> options) : DbContext(options)
{
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public IQueryable<BestSeller> BestSellers(DateTime since)
        => FromExpression(() => BestSellers(since));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(b =>
        {
            b.Property(x => x.Title).HasMaxLength(200);
            b.Property(x => x.Isbn).HasMaxLength(13);
            b.Property(x => x.Price).HasPrecision(10, 2);
            b.HasIndex(x => x.Title);
        });
        modelBuilder.Entity<Author>().Property(x => x.Name).HasMaxLength(100);
        modelBuilder.Entity<Customer>(b =>
        {
            b.Property(x => x.FullName).HasMaxLength(200);
            b.Property(x => x.Email).HasMaxLength(256);
        });
        modelBuilder.Entity<OrderItem>().Property(x => x.UnitPrice).HasPrecision(10, 2);

        modelBuilder.HasDbFunction(typeof(BookstoreFunctions).GetMethod(nameof(BookstoreFunctions.DiscountedPrice))!)
            .HasName("fn_DiscountedPrice")
            .HasSchema("dbo");

        modelBuilder.Entity<BestSeller>().HasNoKey().ToTable((string?)null);
        modelBuilder.HasDbFunction(typeof(BookstoreContext).GetMethod(nameof(BestSellers), [typeof(DateTime)])!)
            .HasName("fn_BestSellers");
    }
}
