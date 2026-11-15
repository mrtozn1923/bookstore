namespace Bookstore.Web.Models;

public sealed record CategoryViewModel(string Name, string ColorName, string Color);

public sealed record BookCardViewModel(
    string Isbn, string Title, string Author, decimal Price, string CoverUrl, bool InStock, CategoryViewModel Category);
