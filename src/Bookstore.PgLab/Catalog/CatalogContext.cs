using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;
using NpgsqlTypes;

namespace Bookstore.PgLab.Catalog;

public enum BookFormat
{
    Paperback,
    Hardcover,
    Ebook,
    Audiobook,
}

public sealed class BookDetails
{
    public int Pages { get; set; }
    public string Language { get; set; } = "en";
    public List<string> FileFormats { get; set; } = [];
}

public sealed class Book
{
    public int BookId { get; set; }
    public Guid PublicId { get; set; }
    public required string Isbn { get; set; }
    public required string Title { get; set; }
    public decimal Price { get; set; }
    public BookFormat Format { get; set; }
    public List<string> Tags { get; set; } = [];
    public BookDetails Details { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public uint Version { get; set; }
    public NpgsqlTsVector SearchVector { get; set; } = null!;
}

public sealed class BookstoreContext(DbContextOptions<BookstoreContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("catalog");
        modelBuilder.Entity<Book>(book =>
        {
            book.HasAlternateKey(b => b.PublicId);
            book.Property(b => b.PublicId).ValueGeneratedOnAdd(); // only primary keys get a generated Guid by default
            book.HasIndex(b => b.Isbn).IsUnique();
            book.Property(b => b.Isbn).HasMaxLength(13);
            book.Property(b => b.Price).HasPrecision(10, 2);
            book.ComplexProperty(b => b.Details, details => details.ToJson());
            book.Property(b => b.Version).IsRowVersion();
            book.HasIndex(b => b.Tags).HasMethod("gin");
            book.HasGeneratedTsVectorColumn(b => b.SearchVector, "english", b => new { b.Title })
                .HasIndex(b => b.SearchVector)
                .HasMethod("gin")
                .IsCreatedConcurrently();
        });
    }

    public static DbContextOptions<BookstoreContext> CreateOptions(string connectionString) =>
        new DbContextOptionsBuilder<BookstoreContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MapEnum<BookFormat>("book_format", "catalog"))
            .UseSnakeCaseNamingConvention()
            .Options;
}

// dotnet ef uses the owner role: migrations run DDL, the application role cannot
public sealed class BookstoreContextFactory : IDesignTimeDbContextFactory<BookstoreContext>
{
    public BookstoreContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("BOOKSTORE_PG_OWNER")
            ?? "Host=localhost;Port=5432;Database=bookstore;Username=bookstore_owner";
        return new BookstoreContext(BookstoreContext.CreateOptions(connectionString));
    }
}
