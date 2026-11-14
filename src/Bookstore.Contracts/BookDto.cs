namespace Bookstore.Contracts;

public sealed record BookDto(string Isbn, string Title, string Author, decimal Price);
