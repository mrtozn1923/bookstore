using Microsoft.EntityFrameworkCore;

namespace Bookstore.Catalog.Data;

// The catalogdb database belongs to this service only; no other service gets its connection string.
public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(book =>
        {
            book.Property(b => b.Isbn).HasMaxLength(13).IsUnicode(false);
            book.HasIndex(b => b.Isbn).IsUnique();
            book.Property(b => b.Title).HasMaxLength(200);
            book.HasIndex(b => b.Title);
            book.Property(b => b.Description).HasMaxLength(1000);
            book.Property(b => b.Price).HasPrecision(10, 2);
            book.Property(b => b.Version).IsConcurrencyToken().HasDefaultValue(1);
            book.Ignore(b => b.Events);
        });
        modelBuilder.Entity<Author>().Property(a => a.Name).HasMaxLength(200);
        modelBuilder.Entity<Category>(category =>
        {
            category.Property(c => c.Name).HasMaxLength(100);
            category.Property(c => c.Slug).HasMaxLength(100).IsUnicode(false);
            category.HasIndex(c => c.Slug).IsUnique();
        });
    }
}
