namespace Bookstore.Api.Books;

// Books live in memory for now; post 02 moves them to SQL Server
public sealed class BookCatalog
{
    private readonly IReadOnlyList<Book> books =
    [
        new("9780441172719", "Dune", "Frank Herbert", 420.00m),
        new("9780141439587", "Emma", "Jane Austen", 260.00m),
        new("9780547928227", "The Hobbit", "J. R. R. Tolkien", 340.00m),
        new("9780141439549", "Middlemarch", "George Eliot", 380.00m),
        new("9780060934347", "Don Quixote", "Miguel de Cervantes", 450.00m),
    ];

    public IReadOnlyList<Book> GetAll() => books;

    public Book? Find(string isbn) => books.FirstOrDefault(b => b.Isbn == isbn);
}
