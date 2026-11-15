using Bookstore.Web.Catalog;

namespace Bookstore.Web.Errors;

public sealed record ServerErrorViewModel(int StatusCode, string ErrorCode, string? UserMessage);

public sealed record NotFoundViewModel(string? OriginalPath, IReadOnlyList<Book> Suggestions);
