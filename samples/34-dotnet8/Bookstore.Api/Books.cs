namespace Bookstore.Api;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public interface IBookService
{
    Book GetById(int id);
}

public class BookNotFoundException(int id) : Exception($"Book {id} was not found.");

public class InMemoryBookService : IBookService
{
    // C# 12 collection expression
    private readonly List<Book> _books =
    [
        new() { Id = 1, Title = "Dune", Price = 450m },
        new() { Id = 2, Title = "Emma", Price = 280m }
    ];

    public Book GetById(int id) =>
        _books.FirstOrDefault(b => b.Id == id) ?? throw new BookNotFoundException(id);
}
