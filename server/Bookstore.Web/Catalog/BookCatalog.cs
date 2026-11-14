namespace Bookstore.Web.Catalog;

public sealed record Book(string Isbn, string Title, string Author, decimal Price);

public sealed class BookCatalog
{
    private static readonly IReadOnlyList<Book> Books =
    [
        new("9780441172719", "Dune", "Frank Herbert", 450m),
        new("9780141439587", "Emma", "Jane Austen", 210m),
        new("9780547928227", "The Hobbit", "J. R. R. Tolkien", 320m),
        new("9780141439549", "Middlemarch", "George Eliot", 280m),
        new("9780141439518", "Pride and Prejudice", "Jane Austen", 195m),
        new("9780141439662", "Sense and Sensibility", "Jane Austen", 185m),
        new("9780141439754", "Silas Marner", "George Eliot", 160m),
    ];

    public IReadOnlyList<Book> GetAll() => Books;
}
