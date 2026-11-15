using System.Collections.Concurrent;

namespace Bookstore.Catalog;

public sealed class BookStock
{
    public required string Isbn { get; init; }
    public required string Title { get; init; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

// In-memory catalog: prices and stock levels the supplier feed updates.
public sealed class CatalogStore
{
    private readonly ConcurrentDictionary<string, BookStock> _books = new(
    [
        new("9780441013593", new BookStock { Isbn = "9780441013593", Title = "Dune", Price = 459.00m, Stock = 12 }),
        new("9780141439587", new BookStock { Isbn = "9780141439587", Title = "Emma", Price = 189.00m, Stock = 30 }),
        new("9780547928227", new BookStock { Isbn = "9780547928227", Title = "The Hobbit", Price = 312.50m, Stock = 8 }),
        new("9780141439549", new BookStock { Isbn = "9780141439549", Title = "Middlemarch", Price = 245.00m, Stock = 5 })
    ]);

    public BookStock? Find(string isbn) => _books.GetValueOrDefault(isbn);

    public IEnumerable<BookStock> All() => _books.Values.OrderBy(b => b.Title);
}
