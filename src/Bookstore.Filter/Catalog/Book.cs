namespace Bookstore.Catalog;

public sealed record Book(string Isbn, string Title, string Author, decimal Price, int Year, int Stock);

public static class SampleBooks
{
    public static IReadOnlyList<Book> All { get; } =
    [
        new("9780441172719", "Dune", "Frank Herbert", 349.90m, 1965, 12),
        new("9780547928227", "The Hobbit", "J.R.R. Tolkien", 289.50m, 1937, 0),
        new("9780141439549", "Middlemarch", "George Eliot", 310.00m, 1871, 4),
        new("9780141439587", "Emma", "Jane Austen", 199.00m, 1815, 7),
        new("9780141439686", "Persuasion", "Jane Austen", 185.00m, 1817, 3),
        new("9780141439662", "Sense and Sensibility", "Jane Austen", 210.00m, 1811, 5),
    ];
}
