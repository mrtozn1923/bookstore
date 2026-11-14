using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Catalog;

// Internal model: StockQuantity, Supplier and Version never leave the API as they are.
public sealed record Book(
    string Isbn,
    string Title,
    string Author,
    string Category,
    decimal Price,
    int StockQuantity,
    string Supplier,
    string? Description = null,
    int Version = 1)
{
    public string ETag => $"\"{Version}\"";
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
    string Author,
    string Category,
    Money Price,
    bool InStock,
    string? Description)
{
    public static BookResponse From(Book book) =>
        new(book.Isbn, book.Title, book.Author, book.Category, new Money(book.Price, "TRY"), book.StockQuantity > 0,
            book.Description);
}
