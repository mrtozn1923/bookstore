using Bookstore.Web.Content;

namespace Bookstore.Web.Catalog;

public enum BookFormat
{
    Paperback,
    Hardcover,
}

// One edition = one ISBN = one product page
public sealed record BookEdition(
    string Isbn,
    string Slug,
    string Title,
    string AuthorName,
    BookFormat Format,
    int NumberOfPages,
    string InLanguage,
    string Category,
    Localized Description,
    decimal ListPrice,
    int StockQuantity,
    IReadOnlyList<string> ImagePaths);

public sealed record Campaign(string Isbn, decimal Price, DateTimeOffset StartsAt, DateTimeOffset EndsAt);

// Sample data: ISBNs have valid check digits but are not real editions; prices and stock are made up
public sealed class BookCatalog
{
    public IReadOnlyList<BookEdition> Books { get; init; } =
    [
        new("9780000000019", "dune-paperback", "Dune", "Frank Herbert", BookFormat.Paperback, 704, "en", "science-fiction",
            new("Çöl gezegeni Arrakis'te geçen bilim kurgu klasiğinin İngilizce karton kapaklı baskısı.",
                "The English paperback edition of the science fiction classic set on the desert planet Arrakis."),
            450.00m, 12,
            ["/images/books/dune-paperback-1x1.jpg", "/images/books/dune-paperback-4x3.jpg", "/images/books/dune-paperback-16x9.jpg"]),
        new("9780000000026", "dune-hardcover", "Dune", "Frank Herbert", BookFormat.Hardcover, 704, "en", "science-fiction",
            new("Çöl gezegeni Arrakis'te geçen bilim kurgu klasiğinin İngilizce ciltli baskısı.",
                "The English hardcover edition of the science fiction classic set on the desert planet Arrakis."),
            890.00m, 0,
            ["/images/books/dune-hardcover-1x1.jpg", "/images/books/dune-hardcover-4x3.jpg", "/images/books/dune-hardcover-16x9.jpg"]),
    ];

    public IReadOnlyList<Campaign> Campaigns { get; init; } =
    [
        new("9780000000019", 360.00m,
            new DateTimeOffset(2026, 11, 10, 0, 0, 0, TimeSpan.FromHours(3)),
            new DateTimeOffset(2026, 11, 30, 23, 59, 59, TimeSpan.FromHours(3))),
    ];

    public BookEdition? FindBySlug(string slug) => Books.FirstOrDefault(b => b.Slug == slug);
}
