namespace Bookstore.Api.Books;

public sealed record BookResponse(string Isbn, string Title, string Author, decimal Price, string Currency);

public sealed record BookDetailResponse(
    string Isbn, string Title, string Author, decimal Price, string Currency, IReadOnlyList<string> MoreByAuthor);

public sealed record CreateBookRequest(string Isbn, string Title, string Author, decimal Price);
