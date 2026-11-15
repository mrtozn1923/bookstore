using Bookstore.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Filter.Data;

public sealed class BookstoreContext(DbContextOptions<BookstoreContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();

    // ToQueryString only needs the provider, it never opens this connection
    public static BookstoreContext ForSqlServer() => new(
        new DbContextOptionsBuilder<BookstoreContext>()
            .UseSqlServer("Server=localhost;Database=BookstoreDb;Trusted_Connection=True;TrustServerCertificate=True")
            .Options);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(book =>
        {
            book.ToTable("Books");
            book.HasKey(b => b.Isbn);
            book.Property(b => b.Isbn).HasMaxLength(13).IsUnicode(false);
            book.Property(b => b.Title).HasMaxLength(200);
            book.Property(b => b.Author).HasMaxLength(100);
            book.Property(b => b.Price).HasPrecision(10, 2);
        });
    }
}
