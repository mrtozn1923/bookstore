namespace Bookstore.Admin.Client.Contracts;

// Mirrors the JSON returned by Bookstore.Api; kept in .Client so WebAssembly components can use it too.
public sealed record BookSummary(string Isbn, string Title, string Author, string Category, decimal Price, int PublishedYear);

public sealed record BookDetails(
    string Isbn, string Title, string Author, string Category, decimal Price, int PublishedYear, string Description, int Available);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public sealed record CategoryView(string Name, string Slug);

// The minimum a browser-side price calculation needs; passed as a serialized component parameter.
public sealed record BookPrice(string Isbn, string Title, decimal Price);

public sealed record AuthorView(int Id, string Name, int BookCount);

public sealed record SetPriceRequest(decimal Price);

public sealed record NewBookRequest(string Isbn, string Title, decimal Price, int PublishedYear, string CategorySlug);

public sealed record NewAuthorRequest(string Name, IReadOnlyList<NewBookRequest> Books);
