namespace Bookstore.Api.Catalog;

public sealed record Book(string Isbn, string Title, string Author, string Category, decimal Price, int Stock)
{
    public string Currency => "TRY";
}
