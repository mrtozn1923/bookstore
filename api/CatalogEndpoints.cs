namespace Bookstore.Assistant.Api;

public static class CatalogEndpoints
{
    // Katalog araması: WebMCP tool'u ve sayfa aynı endpoint'i kullanır
    public static void MapCatalog(this WebApplication app)
    {
        app.MapGet("/catalog/search", (string? q, string? isbn) => Catalog.Books
            .Where(b => isbn is null || b.Isbn == isbn)
            .Where(b => string.IsNullOrWhiteSpace(q)
                || b.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                || b.Author.Contains(q, StringComparison.OrdinalIgnoreCase)
                || b.Genre.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Take(10)
            .Select(b => new BookCard(b.Isbn, b.Title, b.Author, b.PriceTry))
            .ToList());
    }
}
