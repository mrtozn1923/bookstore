namespace Bookstore.Search;

// Deterministic synthetic catalog: same seed, same rows on every machine.
public static class SyntheticBooks
{
    private static readonly string[] Adjectives =
    [
        "Silent", "Hidden", "Last", "Broken", "Golden", "Forgotten", "Distant", "Secret", "Burning", "Quiet",
        "Endless", "Lost", "Northern", "Winter", "Glass", "Iron", "Paper", "Shadow", "Wild", "Little"
    ];

    private static readonly string[] Nouns =
    [
        "River", "Garden", "House", "Road", "Letter", "City", "Island", "Mountain", "Library", "Station",
        "Harbour", "Forest", "Window", "Bridge", "Promise", "Kingdom", "Map", "Season", "Voyage", "Clock"
    ];

    private static readonly string[] FirstNames = ["Ada", "Alan", "Grace", "Linus", "Barbara", "Edsger", "Margaret", "Donald", "Frances", "Ken"];
    private static readonly string[] LastNames = ["Walker", "Hughes", "Turner", "Parker", "Bennett", "Foster", "Hayes", "Morgan", "Reed", "Wells"];
    private static readonly string[] Genres = ["Fiction", "Mystery", "Science Fiction", "Fantasy", "History", "Travel"];

    public static IEnumerable<Book> Generate(int count)
    {
        Random random = new(2026);
        for (int i = 0; i < count; i++)
        {
            string title = random.Next(3) switch
            {
                0 => $"The {Pick(random, Adjectives)} {Pick(random, Nouns)}",
                1 => $"{Pick(random, Nouns)} of the {Pick(random, Adjectives)} {Pick(random, Nouns)}",
                _ => $"A {Pick(random, Nouns)} in {Pick(random, Nouns)} {random.Next(1, 100)}"
            };
            string author = $"{Pick(random, FirstNames)} {Pick(random, LastNames)}";
            string genre = Pick(random, Genres);
            decimal price = random.Next(90, 900) + 0.90m;
            yield return new Book(
                Isbn: Isbn(i),
                Title: title,
                Authors: [new BookAuthor(author, "author")],
                Categories: [genre],
                Language: "en",
                Price: price,
                Currency: "TRY",
                StockQuantity: random.Next(0, 200),
                PublishedOn: new DateOnly(2000, 1, 1).AddDays(random.Next(0, 9_400)),
                AverageRating: Math.Round(2.5m + (decimal)random.NextDouble() * 2.5m, 1),
                Description: $"A {genre.ToLowerInvariant()} book by {author} about a {Pick(random, Adjectives).ToLowerInvariant()} {Pick(random, Nouns).ToLowerInvariant()}.");
        }
    }

    private static string Pick(Random random, string[] values) => values[random.Next(values.Length)];

    // 9782 + 8-digit counter + ISBN-13 check digit.
    private static string Isbn(int counter)
    {
        string body = $"9782{counter:D8}";
        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            sum += (body[i] - '0') * (i % 2 == 0 ? 1 : 3);
        }
        return body + ((10 - sum % 10) % 10);
    }
}
