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
    [property: Range(typeof(decimal), "0.01", "1000000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    decimal Amount,
    [property: Required, StringLength(3, MinimumLength = 3)] string Currency);

// The writable part of a book: what PUT replaces and PATCH merges into.
public sealed record BookFields(
    [property: Required, StringLength(200, MinimumLength = 1)] string Title,
    [property: Required, StringLength(200, MinimumLength = 1)] string Author,
    [property: Required, RegularExpression("^[a-z]+(-[a-z]+)*$")] string Category,
    [property: Required] Money Price,
    [property: StringLength(2000)] string? Description = null);

public sealed record CreateBookRequest(
    [property: Required, RegularExpression("^97[89][0-9]{10}$")] string Isbn,
    [property: Required, StringLength(200, MinimumLength = 1)] string Title,
    [property: Required, StringLength(200, MinimumLength = 1)] string Author,
    [property: Required, RegularExpression("^[a-z]+(-[a-z]+)*$")] string Category,
    [property: Required] Money Price,
    [property: StringLength(2000)] string? Description = null)
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
