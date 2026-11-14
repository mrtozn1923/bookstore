namespace Bookstore.Assistant.Api;

public sealed record Book(string Isbn, string Title, string Author, string Genre, decimal PriceTry, string Note);

public sealed record SourceItem(int Id, string Title, string Url);

// Demo kataloğu; gerçek uygulamada Bookstore.Catalog module'ünden gelir
public static class Catalog
{
    public static readonly IReadOnlyList<Book> Books =
    [
        new("9780441172719", "Dune", "Frank Herbert", "science-fiction", 24.90m, "Desert planet, politics and ecology; long but rewarding."),
        new("9780547928227", "The Hobbit", "J.R.R. Tolkien", "fantasy", 19.90m, "A short, light adventure; a good first fantasy book for teenagers."),
        new("9780141439587", "Emma", "Jane Austen", "classic", 14.50m, "A comedy of manners about a matchmaker who misreads everyone."),
        new("9780062073488", "And Then There Were None", "Agatha Christie", "crime", 16.90m, "Ten strangers on an island; Christie's best-selling mystery."),
        new("9780062693662", "Murder on the Orient Express", "Agatha Christie", "crime", 18.40m, "Poirot solves a murder on a snowbound train."),
        new("9780140437867", "The Hound of the Baskervilles", "Arthur Conan Doyle", "crime", 12.90m, "Sherlock Holmes and a legendary hound on the moors."),
        new("9780394758282", "The Big Sleep", "Raymond Chandler", "crime", 27.00m, "Philip Marlowe in hard-boiled Los Angeles."),
    ];

    // System prompt'a giren numaralı notlar; model bunlara [n] ile atıf yapar
    public static string Notes() => string.Join('\n', Books.Select((b, i) =>
        $"[{i + 1}] {b.Title} by {b.Author} ({b.Genre}, {b.PriceTry} TRY): {b.Note}"));

    public static IReadOnlyList<SourceItem> Sources() =>
        [.. Books.Select((b, i) => new SourceItem(i + 1, b.Title, $"/books/{b.Isbn}"))];
}
