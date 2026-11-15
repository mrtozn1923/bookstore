namespace Bookstore.Api.Catalog;

public sealed record Book(string Isbn, string Title, string Author, decimal Price, string Description);

public sealed record BookSummary(string Isbn, string Summary, string Model, long InputTokens, long OutputTokens);
