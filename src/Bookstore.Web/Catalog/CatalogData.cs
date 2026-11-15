namespace Bookstore.Web.Catalog;

// Same 20 classics as the Bookstore reference app (src/Bookstore.Api/Catalog/CatalogSeed.cs), flattened for the views.
internal static class CatalogData
{
    public static readonly IReadOnlyList<Book> Books =
    [
        new(1, "9780441172719", "Dune", "Frank Herbert", "Science Fiction", 489.00m, 1965, 12),
        new(2, "9780261102217", "The Hobbit", "J.R.R. Tolkien", "Fantasy", 329.90m, 1937, 8),
        new(3, "9780141439587", "Emma", "Jane Austen", "Classics", 249.90m, 1815, 5),
        new(4, "9780141439549", "Middlemarch", "George Eliot", "Classics", 389.00m, 1871, 3),
        new(5, "9780141439518", "Pride and Prejudice", "Jane Austen", "Classics", 239.90m, 1813, 14),
        new(6, "9780451524935", "1984", "George Orwell", "Dystopian", 279.00m, 1949, 20),
        new(7, "9780060850524", "Brave New World", "Aldous Huxley", "Dystopian", 299.00m, 1932, 6),
        new(8, "9780141439471", "Frankenstein", "Mary Shelley", "Classics", 219.90m, 1818, 9),
        new(9, "9780142437247", "Moby-Dick", "Herman Melville", "Classics", 359.00m, 1851, 2),
        new(10, "9780141441146", "Jane Eyre", "Charlotte Brontë", "Classics", 269.90m, 1847, 7),
        new(11, "9780141439556", "Wuthering Heights", "Emily Brontë", "Classics", 249.00m, 1847, 4),
        new(12, "9780141439563", "Great Expectations", "Charles Dickens", "Classics", 289.00m, 1861, 6),
        new(13, "9780143058144", "Crime and Punishment", "Fyodor Dostoevsky", "Classics", 349.00m, 1866, 5),
        new(14, "9780143035008", "Anna Karenina", "Leo Tolstoy", "Classics", 429.00m, 1878, 3),
        new(15, "9781400079988", "War and Peace", "Leo Tolstoy", "Classics", 689.00m, 1869, 0),
        new(16, "9780743273565", "The Great Gatsby", "F. Scott Fitzgerald", "Classics", 229.00m, 1925, 10),
        new(17, "9780553293357", "Foundation", "Isaac Asimov", "Science Fiction", 319.00m, 1951, 11),
        new(18, "9780441478125", "The Left Hand of Darkness", "Ursula K. Le Guin", "Science Fiction", 309.00m, 1969, 4),
        new(19, "9780140449266", "The Count of Monte Cristo", "Alexandre Dumas", "Classics", 459.00m, 1844, 6),
        new(20, "9780060934347", "Don Quixote", "Miguel de Cervantes", "Classics", 399.00m, 1605, 5)
    ];
}
