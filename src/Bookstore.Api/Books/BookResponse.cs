namespace Bookstore.Api.Books;

public sealed record BookResponse(string Isbn, string Title, string Author, decimal Price, string Currency);
