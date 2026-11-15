namespace Bookstore.Web.Models;

public sealed record BookCardViewModel(string Isbn, string Title, string Author, decimal Price, string CoverUrl, bool InStock);
