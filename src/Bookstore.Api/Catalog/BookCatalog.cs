namespace Bookstore.Api.Catalog;

public sealed class BookCatalog
{
    private readonly List<Book> _books =
    [
        new("9780441172719", "Dune", "Frank Herbert", "science-fiction", 420.00m, 12),
        new("9780141439587", "Emma", "Jane Austen", "classics", 185.50m, 0),
        new("9780547928227", "The Hobbit", "J.R.R. Tolkien", "fantasy", 310.00m, 7),
        new("9780141439549", "Middlemarch", "George Eliot", "classics", 265.00m, 3),
        new("9780141439518", "Pride and Prejudice", "Jane Austen", "classics", 170.00m, 21),
    ];

    public IReadOnlyList<Book> All() => _books;

    public Book? Find(string isbn) => _books.FirstOrDefault(b => b.Isbn == isbn);

    public IReadOnlyList<Book> Search(string term) =>
        [.. _books.Where(b =>
            b.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            b.Author.Contains(term, StringComparison.OrdinalIgnoreCase))];
}
