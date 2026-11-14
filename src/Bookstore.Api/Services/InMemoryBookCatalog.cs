using Bookstore.Api.Models;

namespace Bookstore.Api.Services;

public class InMemoryBookCatalog : IBookCatalog
{
    private readonly List<Book> _books = new()
    {
        new Book { Id = 1, Title = "Dune", Price = 450m, CategoryId = 1 },
        new Book { Id = 2, Title = "Emma", Price = 280m, CategoryId = 2 },
        new Book { Id = 3, Title = "The Hobbit", Price = 320m, CategoryId = 1 }
    };
    private readonly object _lock = new();

    public IReadOnlyList<Book> GetAll()
    {
        lock (_lock)
        {
            // We hand out a copy of the list
            return _books.ToList();
        }
    }

    public Book? GetById(int id)
    {
        lock (_lock)
        {
            return _books.FirstOrDefault(b => b.Id == id);
        }
    }

    public Book Add(Book book)
    {
        lock (_lock)
        {
            book.Id = _books.Count == 0 ? 1 : _books.Max(b => b.Id) + 1;
            _books.Add(book);
            return book;
        }
    }
}
