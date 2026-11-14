namespace Bookstore.Catalog;

public sealed record Book(string Isbn, string Title, string Author, decimal Price);

public sealed record BookSummary(string Isbn, string Title);
