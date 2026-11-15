namespace Bookstore.Web.Catalog;

// örnek veri: bellekte bir katalog (gerçek uygulamada BookstoreContext)
public sealed class BookCatalog
{
    public static readonly Genre[] Genres =
    [
        new("science-fiction", "Bilim Kurgu", "Science Fiction"),
        new("fantasy", "Fantastik", "Fantasy"),
        new("classics", "Klasikler", "Classics"),
    ];

    private static readonly (string Title, string Author, int Year)[] SciFi =
    [
        ("Dune", "Frank Herbert", 1965), ("Dune Messiah", "Frank Herbert", 1969), ("Foundation", "Isaac Asimov", 1951),
        ("Foundation and Empire", "Isaac Asimov", 1952), ("Second Foundation", "Isaac Asimov", 1953),
        ("I, Robot", "Isaac Asimov", 1950), ("Neuromancer", "William Gibson", 1984), ("Hyperion", "Dan Simmons", 1989),
        ("The Left Hand of Darkness", "Ursula K. Le Guin", 1969), ("The Dispossessed", "Ursula K. Le Guin", 1974),
        ("The Time Machine", "H. G. Wells", 1895), ("The War of the Worlds", "H. G. Wells", 1898),
        ("Brave New World", "Aldous Huxley", 1932), ("Fahrenheit 451", "Ray Bradbury", 1953),
        ("The Martian Chronicles", "Ray Bradbury", 1950), ("Childhood's End", "Arthur C. Clarke", 1953),
        ("Rendezvous with Rama", "Arthur C. Clarke", 1973), ("Solaris", "Stanisław Lem", 1961),
        ("Ubik", "Philip K. Dick", 1969), ("Ringworld", "Larry Niven", 1970), ("Ender's Game", "Orson Scott Card", 1985),
        ("Snow Crash", "Neal Stephenson", 1992), ("The Stars My Destination", "Alfred Bester", 1956),
        ("Flowers for Algernon", "Daniel Keyes", 1966), ("The Forever War", "Joe Haldeman", 1974),
        ("Kindred", "Octavia E. Butler", 1979), ("Gateway", "Frederik Pohl", 1977),
        ("A Canticle for Leibowitz", "Walter M. Miller Jr.", 1959), ("Cat's Cradle", "Kurt Vonnegut", 1963),
        ("The Moon Is a Harsh Mistress", "Robert A. Heinlein", 1966),
    ];

    private static readonly (string Title, string Author, int Year, string Genre, string Language)[] Others =
    [
        ("The Hobbit", "J. R. R. Tolkien", 1937, "fantasy", "en"), ("A Wizard of Earthsea", "Ursula K. Le Guin", 1968, "fantasy", "en"),
        ("The Last Unicorn", "Peter S. Beagle", 1968, "fantasy", "en"), ("Emma", "Jane Austen", 1815, "classics", "en"),
        ("Middlemarch", "George Eliot", 1871, "classics", "en"), ("Jane Eyre", "Charlotte Brontë", 1847, "classics", "en"),
        ("Şeker Portakalı", "José Mauro de Vasconcelos", 1968, "classics", "tr"),
        ("Saatleri Ayarlama Enstitüsü", "Ahmet Hamdi Tanpınar", 1961, "classics", "tr"),
    ];

    public IReadOnlyList<Book> Books { get; }

    public BookCatalog()
    {
        BookFormat[] formats = [BookFormat.Hardcover, BookFormat.Paperback, BookFormat.Ebook];
        List<Book> books = [];
        int n = 0;
        foreach ((string title, string author, int year) in SciFi)
            books.Add(Create(title, author, year, "science-fiction", n % 5 == 4 ? "tr" : "en", formats[n % 3], n++));
        foreach ((string title, string author, int year, string genre, string language) in Others)
            books.Add(Create(title, author, year, genre, language, formats[n % 3], n++));
        Books = books;
    }

    public Book? Find(string slug) => Books.FirstOrDefault(b => b.Slug == slug);

    private static Book Create(string title, string author, int year, string genre, string language, BookFormat format, int n) =>
        new($"978{n:D10}", Urls.Slugifier.ToSlug(title), title, author, genre, format, language, 180m + n * 15m, year);
}
