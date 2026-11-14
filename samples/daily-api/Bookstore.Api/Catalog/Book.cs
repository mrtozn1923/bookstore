namespace Bookstore.Api.Catalog;

public record Book(int Id, string Isbn, string Title, decimal Price, string Category, string? Series = null);

public class BookCatalog
{
    private readonly List<Book> _books =
    [
        new(1, "978-0441013593", "Dune", 450m, "science-fiction", "Dune Chronicles"),
        new(2, "978-0141439587", "Emma", 280m, "classics"),
        new(3, "978-0547928227", "The Hobbit", 320m, "science-fiction"),
        new(4, "978-0141439549", "Middlemarch", 390m, "classics"),
    ];

    public IReadOnlyList<Book> All => _books;

    public Book? FindByIsbn(string isbn) => _books.FirstOrDefault(b => b.Isbn == isbn);

    public Book? Find(Isbn isbn) => _books.FirstOrDefault(b => b.Isbn.Replace("-", "") == isbn.Value);

    public void Replace(Book book)
    {
        lock (_books)
        {
            int index = _books.FindIndex(b => b.Id == book.Id);
            _books[index] = book;
        }
    }
}
