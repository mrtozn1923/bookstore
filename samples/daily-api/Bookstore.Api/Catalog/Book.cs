namespace Bookstore.Api.Catalog;

public record Book(int Id, string Isbn, string Title, decimal Price, int CategoryId);

public class BookCatalog
{
    private readonly List<Book> _books =
    [
        new(1, "978-0441013593", "Dune", 450m, 1),
        new(2, "978-0141439587", "Emma", 280m, 2),
        new(3, "978-0547928227", "The Hobbit", 320m, 1),
        new(4, "978-0141439549", "Middlemarch", 390m, 2),
    ];

    public IReadOnlyList<Book> All => _books;

    public Book? FindByIsbn(string isbn) => _books.FirstOrDefault(b => b.Isbn == isbn);
}
