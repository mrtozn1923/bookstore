namespace Bookstore.Catalog;

public sealed class InMemoryBookCatalog(IEnumerable<Book> books) : IBookCatalog
{
    private readonly List<Book> _books = [.. books];

    public InMemoryBookCatalog()
        : this(
        [
            new("9780441172719", "Dune", "Frank Herbert", 349.90m),
            new("9780547928227", "The Hobbit", "J. R. R. Tolkien", 289.50m),
            new("9780141439518", "Emma", "Jane Austen", 189.00m),
        ])
    {
    }

    public IReadOnlyList<Book> GetAll() => _books;

    public Book? FindByIsbn(string isbn) => _books.Find(b => b.Isbn == isbn);
}
