namespace Bookstore.Admin.Client.Contracts;

// Mirrors the JSON returned by Bookstore.Api; kept in .Client so WebAssembly components can use it too.
public sealed record BookSummary(string Isbn, string Title, string Author, string Category, decimal Price, int PublishedYear);

public sealed record BookDetails(
    string Isbn, string Title, string Author, string Category, decimal Price, int PublishedYear, string Description, int Available);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
