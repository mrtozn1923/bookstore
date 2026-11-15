namespace Bookstore.Api.Catalog;

public interface IBookRepository
{
    Book? FindByIsbn(string isbn);
    IReadOnlyList<Book> Search(string query, decimal? maxPrice);
    IReadOnlyList<Book> All();
}

// A small in-memory catalog; persistence is not this series' topic.
public sealed class InMemoryBookRepository : IBookRepository
{
    private static readonly Book[] Books =
    [
        new("9780441172719", "Dune", "Frank Herbert", 420.00m,
            "On the desert planet Arrakis, young Paul Atreides is caught in a struggle over the spice that controls the galaxy."),
        new("9780141439587", "Emma", "Jane Austen", 185.00m,
            "Emma Woodhouse, clever and rich, plays matchmaker in her village and misreads almost everyone around her."),
        new("9780547928227", "The Hobbit", "J. R. R. Tolkien", 310.00m,
            "Bilbo Baggins leaves his quiet hole to help a band of dwarves reclaim their mountain home from a dragon."),
        new("9780141439549", "Middlemarch", "George Eliot", 265.00m,
            "The lives of the people of a provincial English town cross as they chase marriage, reform and vocation."),
    ];

    public Book? FindByIsbn(string isbn) => Books.FirstOrDefault(b => b.Isbn == isbn);

    public IReadOnlyList<Book> All() => Books;

    public IReadOnlyList<Book> Search(string query, decimal? maxPrice) =>
        Books.Where(b => (b.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                          || b.Author.Contains(query, StringComparison.OrdinalIgnoreCase)
                          || b.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
                         && (maxPrice is null || b.Price <= maxPrice))
             .ToList();
}
