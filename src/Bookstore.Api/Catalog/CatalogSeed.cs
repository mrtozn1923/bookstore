namespace Bookstore.Catalog;

// Seed data that ships with the first migration: 20 classics, 18 authors, 4 categories.
internal static class CatalogSeed
{
    public static readonly Category[] Categories =
    [
        new() { Id = 1, Name = "Classics", Slug = "classics" },
        new() { Id = 2, Name = "Science Fiction", Slug = "science-fiction" },
        new() { Id = 3, Name = "Fantasy", Slug = "fantasy" },
        new() { Id = 4, Name = "Dystopian", Slug = "dystopian" }
    ];

    public static readonly Author[] Authors =
    [
        new() { Id = 1, Name = "Frank Herbert" },
        new() { Id = 2, Name = "J.R.R. Tolkien" },
        new() { Id = 3, Name = "Jane Austen" },
        new() { Id = 4, Name = "George Eliot" },
        new() { Id = 5, Name = "George Orwell" },
        new() { Id = 6, Name = "Aldous Huxley" },
        new() { Id = 7, Name = "Mary Shelley" },
        new() { Id = 8, Name = "Herman Melville" },
        new() { Id = 9, Name = "Charlotte Brontë" },
        new() { Id = 10, Name = "Emily Brontë" },
        new() { Id = 11, Name = "Charles Dickens" },
        new() { Id = 12, Name = "Fyodor Dostoevsky" },
        new() { Id = 13, Name = "Leo Tolstoy" },
        new() { Id = 14, Name = "F. Scott Fitzgerald" },
        new() { Id = 15, Name = "Isaac Asimov" },
        new() { Id = 16, Name = "Ursula K. Le Guin" },
        new() { Id = 17, Name = "Alexandre Dumas" },
        new() { Id = 18, Name = "Miguel de Cervantes" }
    ];

    public static readonly Book[] Books =
    [
        Book(1, "9780441172719", "Dune", 1, 2, 1965, 489.00m, "A desert planet, a noble family and the most valuable substance in the universe."),
        Book(2, "9780261102217", "The Hobbit", 2, 3, 1937, 329.90m, "Bilbo Baggins leaves his comfortable home for an adventure with thirteen dwarves."),
        Book(3, "9780141439587", "Emma", 3, 1, 1815, 249.90m, "A young woman in a small English village keeps arranging other people's marriages."),
        Book(4, "9780141439549", "Middlemarch", 4, 1, 1871, 389.00m, "Several intertwined lives in a provincial English town during a time of reform."),
        Book(5, "9780141439518", "Pride and Prejudice", 3, 1, 1813, 239.90m, "Elizabeth Bennet and Mr Darcy misjudge each other, then slowly change their minds."),
        Book(6, "9780451524935", "1984", 5, 4, 1949, 279.00m, "Winston Smith works for a state that rewrites history and watches everyone."),
        Book(7, "9780060850524", "Brave New World", 6, 4, 1932, 299.00m, "A society kept stable by engineered birth, conditioning and constant pleasure."),
        Book(8, "9780141439471", "Frankenstein", 7, 1, 1818, 219.90m, "A scientist creates life and then abandons what he has made."),
        Book(9, "9780142437247", "Moby-Dick", 8, 1, 1851, 359.00m, "Captain Ahab hunts the white whale that took his leg."),
        Book(10, "9780141441146", "Jane Eyre", 9, 1, 1847, 269.90m, "An orphan becomes a governess and refuses to give up her independence."),
        Book(11, "9780141439556", "Wuthering Heights", 10, 1, 1847, 249.00m, "A destructive love story on the Yorkshire moors across two generations."),
        Book(12, "9780141439563", "Great Expectations", 11, 1, 1861, 289.00m, "Pip, an orphan, receives a fortune from an unknown benefactor."),
        Book(13, "9780143058144", "Crime and Punishment", 12, 1, 1866, 349.00m, "A poor student commits a murder and cannot escape his own conscience."),
        Book(14, "9780143035008", "Anna Karenina", 13, 1, 1878, 429.00m, "A married woman's affair collides with the rules of Russian high society."),
        Book(15, "9781400079988", "War and Peace", 13, 1, 1869, 689.00m, "Russian families live through the Napoleonic Wars."),
        Book(16, "9780743273565", "The Great Gatsby", 14, 1, 1925, 229.00m, "A mysterious millionaire throws parties to win back a lost love."),
        Book(17, "9780553293357", "Foundation", 15, 2, 1951, 319.00m, "A mathematician predicts the fall of a galactic empire and plans for what comes after."),
        Book(18, "9780441478125", "The Left Hand of Darkness", 16, 2, 1969, 309.00m, "An envoy visits a planet whose people have no fixed gender."),
        Book(19, "9780140449266", "The Count of Monte Cristo", 17, 1, 1844, 459.00m, "A wrongly imprisoned sailor escapes and plans his revenge."),
        Book(20, "9780060934347", "Don Quixote", 18, 1, 1605, 399.00m, "A man who has read too many chivalric romances sets out to become a knight.")
    ];

    private static Book Book(int id, string isbn, string title, int authorId, int categoryId, int year, decimal price, string description) =>
        new()
        {
            Id = id,
            Isbn = isbn,
            Title = title,
            AuthorId = authorId,
            CategoryId = categoryId,
            PublishedYear = year,
            Price = price,
            Description = description
        };
}
