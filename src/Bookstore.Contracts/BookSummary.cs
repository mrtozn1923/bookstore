namespace Bookstore.Contracts;

public sealed record BookSummary(Isbn Isbn, string Title, string Author, decimal Price, string Currency = "TRY");
