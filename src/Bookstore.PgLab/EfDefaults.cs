using Microsoft.EntityFrameworkCore;

namespace Bookstore.PgLab;

public sealed class Author
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<Book> Books { get; set; } = [];
}

public sealed class Book
{
    public int Id { get; set; }
    public string Isbn { get; set; } = "";
    public string Title { get; set; } = "";
    public decimal Price { get; set; }
    public DateTime PublishedAt { get; set; }
    public int AuthorId { get; set; }
    public Author? Author { get; set; }
}

// EF Core defaults, no naming convention: what the SQL Server model produces on PostgreSQL
public sealed class BookstoreContext(DbContextOptions<BookstoreContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Author> Authors => Set<Author>();
}

public static class EfDefaults
{
    public static void PrintCreateScript(string connectionString)
    {
        DbContextOptions<BookstoreContext> options = new DbContextOptionsBuilder<BookstoreContext>()
            .UseNpgsql(connectionString)
            .Options;
        using BookstoreContext db = new(options);
        Console.WriteLine(db.Database.GenerateCreateScript());
    }
}
