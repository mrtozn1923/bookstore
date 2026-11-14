namespace Bookstore.Api.Books;

public sealed record Book(string Isbn, string Title, string Author, decimal Price, string Currency = "TRY");
