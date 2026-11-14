namespace Bookstore.Api.Catalog;

// Internal model: StockQuantity and Supplier never leave the API as they are.
public sealed record Book(
    string Isbn,
    string Title,
    string Author,
    string Category,
    decimal Price,
    int StockQuantity,
    string Supplier);

public sealed record Money(decimal Amount, string Currency);

public sealed record BookResponse(
    string Isbn,
    string Title,
    string Author,
    string Category,
    Money Price,
    bool InStock)
{
    public static BookResponse From(Book book) =>
        new(book.Isbn, book.Title, book.Author, book.Category, new Money(book.Price, "TRY"), book.StockQuantity > 0);
}

public sealed record BookListResponse(IReadOnlyList<BookResponse> Items, string? NextPageToken);
