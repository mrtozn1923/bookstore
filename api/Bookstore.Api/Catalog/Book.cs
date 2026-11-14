namespace Bookstore.Api.Catalog;

public sealed record Book(
    string Isbn,
    string Title,
    string Author,
    string Publisher,
    int Year,
    decimal Price,
    string Currency,
    int Stock,
    string Category,
    string? CoverUrl,
    string Summary);
