using System.ComponentModel.DataAnnotations;
using Bookstore.Api.Common;

namespace Bookstore.Api.Catalog;

// Internal model: StockQuantity, Supplier and Version never leave the API as they are.
public sealed record Book(
    string Isbn,
    string Title,
    IReadOnlyList<string> Authors,
    string Category,
    decimal Price,
    int StockQuantity,
    string Supplier,
    string? Description = null,
    int Version = 1)
{
    public string ETag => $"\"{Version}\"";

    // The single author string of api-version 2026-09-01: "Terry Pratchett, Neil Gaiman".
    public string AuthorLine => string.Join(", ", Authors);
}

public sealed record Money(
    [property: Range(typeof(decimal), "0.01", "1000000", ErrorMessage = "out-of-range",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    decimal Amount,
    [property: Required(ErrorMessage = "required"), StringLength(3, MinimumLength = 3, ErrorMessage = "invalid-length")]
    string Currency);

// The writable part of a book: what PUT replaces and PATCH merges into.
public sealed record BookFields(
    [property: Required(ErrorMessage = "required"), StringLength(200, MinimumLength = 1, ErrorMessage = "invalid-length")]
    string Title,
    [property: Required(ErrorMessage = "required"), StringLength(200, MinimumLength = 1, ErrorMessage = "invalid-length")]
    string Author,
    [property: Required(ErrorMessage = "required"), RegularExpression("^[a-z]+(-[a-z]+)*$", ErrorMessage = "invalid-format")]
    string Category,
    [property: Required(ErrorMessage = "required")]
    Money Price,
    [property: StringLength(2000, ErrorMessage = "invalid-length")]
    string? Description = null);

public sealed record BookLookupRequest(
    [property: Required(ErrorMessage = "required")]
    string[] Isbns);

public sealed record CreateBookRequest(
    [property: Required(ErrorMessage = "required"), RegularExpression("^97[89][0-9]{10}$", ErrorMessage = "invalid-format")]
    string Isbn,
    [property: Required(ErrorMessage = "required"), StringLength(200, MinimumLength = 1, ErrorMessage = "invalid-length")]
    string Title,
    [property: Required(ErrorMessage = "required"), StringLength(200, MinimumLength = 1, ErrorMessage = "invalid-length")]
    string Author,
    [property: Required(ErrorMessage = "required"), RegularExpression("^[a-z]+(-[a-z]+)*$", ErrorMessage = "invalid-format")]
    string Category,
    [property: Required(ErrorMessage = "required")]
    Money Price,
    [property: StringLength(2000, ErrorMessage = "invalid-length")]
    string? Description = null)
{
    public BookFields ToFields() => new(Title, Author, Category, Price, Description);
}

public sealed record BookResponse(
    string Isbn,
    string Title,
    [property: Obsolete("Only in api-version 2026-09-01; use Authors.")] string? Author,
    IReadOnlyList<string> Authors,
    string Category,
    Money Price,
    bool InStock,
    string? Description)
{
    // author was replaced by authors in 2026-11-15; null values are omitted from the JSON.
    public static BookResponse From(Book book, ApiVersion version) =>
        new(book.Isbn, book.Title, version == ApiVersions.V20260901 ? book.AuthorLine : null, book.Authors,
            book.Category, new Money(book.Price, "TRY"), book.StockQuantity > 0, book.Description);
}
