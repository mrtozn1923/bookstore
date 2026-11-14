using Microsoft.EntityFrameworkCore;

namespace Bookstore.Data;

public sealed class BookstoreContext(DbContextOptions<BookstoreContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(book =>
        {
            book.HasKey(b => b.Isbn);
            book.Property(b => b.Isbn).HasMaxLength(13).IsFixedLength();
            book.Property(b => b.Title).HasMaxLength(200);
            book.Property(b => b.Author).HasMaxLength(200);
            book.Property(b => b.Price).HasPrecision(10, 2);
            book.Property(b => b.Currency).HasMaxLength(3).IsFixedLength();
        });
    }
}
