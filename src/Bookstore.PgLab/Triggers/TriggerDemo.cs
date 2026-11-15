using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookstore.PgLab.Triggers;

public sealed class Book
{
    public int BookId { get; set; }
    public string Title { get; set; } = "";
    public decimal Price { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// post 23: books.updated_at is set by the BEFORE UPDATE trigger trg_books_updated_at
public abstract class BooksContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();

    protected abstract bool ReadTriggerValues { get; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(book =>
        {
            book.ToTable("books");
            book.Property(b => b.BookId).HasColumnName("book_id");
            book.Property(b => b.Title).HasColumnName("title");
            book.Property(b => b.Price).HasColumnName("price");
            PropertyBuilder<DateTime> updatedAt = book.Property(b => b.UpdatedAt).HasColumnName("updated_at");
            if (ReadTriggerValues)
            {
                updatedAt.ValueGeneratedOnAddOrUpdate();
            }
        });
    }
}

public sealed class NaiveBooksContext(DbContextOptions<NaiveBooksContext> options) : BooksContext(options)
{
    protected override bool ReadTriggerValues => false;
}

public sealed class TriggerAwareBooksContext(DbContextOptions<TriggerAwareBooksContext> options) : BooksContext(options)
{
    protected override bool ReadTriggerValues => true;
}

public static class TriggerDemo
{
    public static async Task RunAsync(string connectionString)
    {
        await using (NaiveBooksContext naive = new(Options<NaiveBooksContext>(connectionString)))
        {
            await ChangePriceAsync(naive, "naive");
        }
        await using (TriggerAwareBooksContext aware = new(Options<TriggerAwareBooksContext>(connectionString)))
        {
            await ChangePriceAsync(aware, "trigger-aware");
        }
    }

    private static async Task ChangePriceAsync(BooksContext db, string label)
    {
        Book book = await db.Books.SingleAsync(b => b.BookId == 10);
        DateTime before = book.UpdatedAt;
        book.Price += 1;
        Console.WriteLine($"-- {label}");
        await db.SaveChangesAsync();

        DateTime inDatabase = await db.Books.Where(b => b.BookId == 10).Select(b => b.UpdatedAt).SingleAsync();
        Console.WriteLine($"entity: {book.UpdatedAt:HH:mm:ss.fff} (before {before:HH:mm:ss.fff}), database: {inDatabase:HH:mm:ss.fff}");
    }

    private static DbContextOptions<T> Options<T>(string connectionString) where T : DbContext =>
        new DbContextOptionsBuilder<T>()
            .UseNpgsql(connectionString)
            .LogTo(Console.WriteLine, [RelationalEventId.CommandExecuted], Microsoft.Extensions.Logging.LogLevel.Information,
                DbContextLoggerOptions.None)
            .Options;
}
