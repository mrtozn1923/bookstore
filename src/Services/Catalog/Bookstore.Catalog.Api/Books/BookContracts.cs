using System.ComponentModel.DataAnnotations;

namespace Bookstore.Catalog.Books;

// The public HTTP contract of Catalog. Entities never leave the service; these records do.
public sealed record BookSummaryResponse(
    string Isbn, string Title, string Author, string Category, decimal Price, string Currency, int PublishedYear, int Version);

public sealed record BookResponse(
    string Isbn, string Title, string Author, string Category, decimal Price, string Currency,
    int PublishedYear, string Description, int Version);

public sealed record CategoryResponse(string Name, string Slug);

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed record ChangePriceRequest([property: Range(typeof(decimal), "0.01", "100000")] decimal Price);

public sealed record RenameBookRequest([property: Required, StringLength(200, MinimumLength = 1)] string Title);
