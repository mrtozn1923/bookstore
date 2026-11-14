namespace Bookstore.Api.Catalog;

public sealed class BookCatalog
{
    private static readonly (string Title, string Author, string Category)[] Classics =
    [
        ("Dune", "Frank Herbert", "Science Fiction"),
        ("Emma", "Jane Austen", "Classics"),
        ("The Hobbit", "J. R. R. Tolkien", "Fantasy"),
        ("Middlemarch", "George Eliot", "Classics"),
        ("Frankenstein", "Mary Shelley", "Horror"),
        ("Pride and Prejudice", "Jane Austen", "Classics"),
        ("Moby-Dick", "Herman Melville", "Adventure"),
        ("Jane Eyre", "Charlotte Bronte", "Classics"),
        ("War and Peace", "Leo Tolstoy", "Classics"),
        ("Dracula", "Bram Stoker", "Horror"),
        ("The Odyssey", "Homer", "Poetry"),
        ("Don Quixote", "Miguel de Cervantes", "Adventure"),
        ("Wuthering Heights", "Emily Bronte", "Classics"),
        ("Great Expectations", "Charles Dickens", "Classics"),
        ("Crime and Punishment", "Fyodor Dostoevsky", "Classics"),
        ("Anna Karenina", "Leo Tolstoy", "Classics"),
        ("The Iliad", "Homer", "Poetry"),
        ("Les Miserables", "Victor Hugo", "Classics"),
        ("The Count of Monte Cristo", "Alexandre Dumas", "Adventure"),
        ("Treasure Island", "Robert Louis Stevenson", "Adventure"),
        ("The Time Machine", "H. G. Wells", "Science Fiction"),
        ("The War of the Worlds", "H. G. Wells", "Science Fiction"),
        ("Little Women", "Louisa May Alcott", "Classics"),
        ("Sense and Sensibility", "Jane Austen", "Classics"),
        ("The Brothers Karamazov", "Fyodor Dostoevsky", "Classics"),
    ];

    private static readonly string[] Publishers =
        ["Penguin Classics", "Oxford World's Classics", "Vintage", "Everyman's Library", "Wordsworth", "Modern Library", "Signet", "Bantam"];

    // Only the first 12 classics have a cover image (Covers/ folder, outside wwwroot)
    private const int BooksWithCover = 12;
    public const int CatalogSize = 2000;

    public BookCatalog()
    {
        List<Book> books = new(CatalogSize);
        for (int i = 0; i < CatalogSize; i++)
        {
            (string title, string author, string category) = Classics[i % Classics.Length];
            int edition = i / Classics.Length;
            string publisher = Publishers[(i * 7 + edition) % Publishers.Length];
            // Editions published between 1990 and 2025
            int year = 1990 + (i * 13 + edition * 5) % 36;
            string isbn = Isbn13("978" + (100000000 + i * 37).ToString());
            string? cover = edition == 0 && i < BooksWithCover ? $"/covers/{Slug(title)}.jpg" : null;
            decimal price = 180m + (i * 37 % 420);
            int stock = (i * 11 + 7) % 40;
            string summary = $"{title} by {author}, {publisher} edition ({year}).";
            books.Add(new Book(isbn, title, author, publisher, year, price, "TRY", stock, category, cover, summary));
        }
        All = books;
        ByIsbn = books.ToDictionary(b => b.Isbn);
    }

    public IReadOnlyList<Book> All { get; }
    public IReadOnlyDictionary<string, Book> ByIsbn { get; }
    public Book Featured => All[0];
    public IEnumerable<Book> NewArrivals => All.Skip(1).Take(8);

    public static string Slug(string title) =>
        new string(title.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());

    private static string Isbn13(string first12)
    {
        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            sum += (first12[i] - '0') * (i % 2 == 0 ? 1 : 3);
        }
        return first12 + ((10 - sum % 10) % 10);
    }
}
