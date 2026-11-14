using Microsoft.EntityFrameworkCore;

namespace Bookstore.Catalog.Data;

// Sample data for local development: the same 20 classics as the reference app.
public static class CatalogSeed
{
    public static async Task SeedAsync(CatalogDbContext db, CancellationToken ct)
    {
        if (await db.Books.AnyAsync(ct))
        {
            return;
        }

        Dictionary<string, Category> categories = new()
        {
            ["classics"] = new() { Name = "Classics", Slug = "classics" },
            ["science-fiction"] = new() { Name = "Science Fiction", Slug = "science-fiction" },
            ["fantasy"] = new() { Name = "Fantasy", Slug = "fantasy" },
            ["dystopian"] = new() { Name = "Dystopian", Slug = "dystopian" }
        };
        Dictionary<string, Author> authors = [];

        foreach ((string isbn, string title, string author, string category, int year, decimal price, string description) in Books)
        {
            if (!authors.TryGetValue(author, out Author? existing))
            {
                existing = new Author { Name = author };
                authors[author] = existing;
            }
            db.Books.Add(new Book
            {
                Isbn = isbn,
                Title = title,
                Author = existing,
                Category = categories[category],
                PublishedYear = year,
                Price = price,
                Description = description
            });
        }
        await db.SaveChangesAsync(ct);
    }

    private static readonly (string Isbn, string Title, string Author, string Category, int Year, decimal Price, string Description)[] Books =
    [
        ("9780441172719", "Dune", "Frank Herbert", "science-fiction", 1965, 489.00m, "A desert planet, a noble family and the most valuable substance in the universe."),
        ("9780261102217", "The Hobbit", "J.R.R. Tolkien", "fantasy", 1937, 329.90m, "Bilbo Baggins leaves his comfortable home for an adventure with thirteen dwarves."),
        ("9780141439587", "Emma", "Jane Austen", "classics", 1815, 249.90m, "A young woman in a small English village keeps arranging other people's marriages."),
        ("9780141439549", "Middlemarch", "George Eliot", "classics", 1871, 389.00m, "Several intertwined lives in a provincial English town during a time of reform."),
        ("9780141439518", "Pride and Prejudice", "Jane Austen", "classics", 1813, 239.90m, "Elizabeth Bennet and Mr Darcy misjudge each other, then slowly change their minds."),
        ("9780451524935", "1984", "George Orwell", "dystopian", 1949, 279.00m, "Winston Smith works for a state that rewrites history and watches everyone."),
        ("9780060850524", "Brave New World", "Aldous Huxley", "dystopian", 1932, 299.00m, "A society kept stable by engineered birth, conditioning and constant pleasure."),
        ("9780141439471", "Frankenstein", "Mary Shelley", "classics", 1818, 219.90m, "A scientist creates life and then abandons what he has made."),
        ("9780142437247", "Moby-Dick", "Herman Melville", "classics", 1851, 359.00m, "Captain Ahab hunts the white whale that took his leg."),
        ("9780141441146", "Jane Eyre", "Charlotte Brontë", "classics", 1847, 269.90m, "An orphan becomes a governess and refuses to give up her independence."),
        ("9780141439556", "Wuthering Heights", "Emily Brontë", "classics", 1847, 249.00m, "A destructive love story on the Yorkshire moors across two generations."),
        ("9780141439563", "Great Expectations", "Charles Dickens", "classics", 1861, 289.00m, "Pip, an orphan, receives a fortune from an unknown benefactor."),
        ("9780143058144", "Crime and Punishment", "Fyodor Dostoevsky", "classics", 1866, 349.00m, "A poor student commits a murder and cannot escape his own conscience."),
        ("9780143035008", "Anna Karenina", "Leo Tolstoy", "classics", 1878, 429.00m, "A married woman's affair collides with the rules of Russian high society."),
        ("9781400079988", "War and Peace", "Leo Tolstoy", "classics", 1869, 689.00m, "Russian families live through the Napoleonic Wars."),
        ("9780743273565", "The Great Gatsby", "F. Scott Fitzgerald", "classics", 1925, 229.00m, "A mysterious millionaire throws parties to win back a lost love."),
        ("9780553293357", "Foundation", "Isaac Asimov", "science-fiction", 1951, 319.00m, "A mathematician predicts the fall of a galactic empire and plans for what comes after."),
        ("9780441478125", "The Left Hand of Darkness", "Ursula K. Le Guin", "science-fiction", 1969, 309.00m, "An envoy visits a planet whose people have no fixed gender."),
        ("9780140449266", "The Count of Monte Cristo", "Alexandre Dumas", "classics", 1844, 459.00m, "A wrongly imprisoned sailor escapes and plans his revenge."),
        ("9780060934347", "Don Quixote", "Miguel de Cervantes", "classics", 1605, 399.00m, "A man who has read too many chivalric romances sets out to become a knight.")
    ];
}
