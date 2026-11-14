namespace Bookstore.Api;

public class InMemoryBookService : IBookService
{
    private readonly List<Book> _books = new()
    {
        new Book { Id = 1, Title = "Dune", Price = 450m },
        new Book { Id = 2, Title = "Emma", Price = 280m },
        new Book { Id = 3, Title = "The Hobbit", Price = 320m }
    };

    public IEnumerable<Book> GetAll() => _books;

    public Book? GetById(int id) => _books.FirstOrDefault(b => b.Id == id);

    public Book Add(string title, decimal price)
    {
        var book = new Book { Id = _books.Max(b => b.Id) + 1, Title = title, Price = price };
        _books.Add(book);
        return book;
    }
}
