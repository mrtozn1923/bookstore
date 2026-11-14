using System.Data;
using System.Diagnostics;
using Bookstore.Search;
using Microsoft.Data.SqlClient;

namespace Bookstore.SearchLab;

// Creates dbo.Books / dbo.BookAuthors and fills them: data/books.json plus synthetic rows.
public static class SqlSeeder
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

    public static async Task RunAsync(string[] args)
    {
        int syntheticCount = args.Length > 0 ? int.Parse(args[0]) : 200_000;
        string connectionString = Environment.GetEnvironmentVariable("BOOKSTORE_SQL")
            ?? throw new InvalidOperationException("Set BOOKSTORE_SQL to a SQL Server connection string.");

        Stopwatch watch = Stopwatch.StartNew();
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();

        string schema = await File.ReadAllTextAsync(Path.Combine("sql", "schema.sql"));
        foreach (string batch in schema.Split("\nGO", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            await using SqlCommand command = new(batch, connection);
            await command.ExecuteNonQueryAsync();
        }

        IReadOnlyList<Book> curated = await BookCatalog.LoadAsync(Path.Combine("data", "books.json"));
        List<Book> all = [.. curated, .. Synthetic(syntheticCount)];

        DataTable books = new();
        books.Columns.Add("BookId", typeof(int));
        books.Columns.Add("Isbn", typeof(string));
        books.Columns.Add("Title", typeof(string));
        books.Columns.Add("Language", typeof(string));
        books.Columns.Add("Categories", typeof(string));
        books.Columns.Add("Price", typeof(decimal));
        books.Columns.Add("StockQuantity", typeof(int));
        books.Columns.Add("PublishedOn", typeof(DateTime));
        books.Columns.Add("AverageRating", typeof(decimal));
        books.Columns.Add("Description", typeof(string));

        DataTable authors = new();
        authors.Columns.Add("BookId", typeof(int));
        authors.Columns.Add("Position", typeof(byte));
        authors.Columns.Add("Name", typeof(string));
        authors.Columns.Add("Role", typeof(string));

        int bookId = 0;
        foreach (Book book in all)
        {
            bookId++;
            books.Rows.Add(bookId, book.Isbn, book.Title, book.Language, string.Join(',', book.Categories), book.Price,
                book.StockQuantity, book.PublishedOn.ToDateTime(TimeOnly.MinValue), (object?)book.AverageRating ?? DBNull.Value,
                book.Description);
            for (int i = 0; i < book.Authors.Count; i++)
            {
                authors.Rows.Add(bookId, (byte)(i + 1), book.Authors[i].Name, book.Authors[i].Role);
            }
        }

        // KeepIdentity: BookId values above become the real identity values.
        using (SqlBulkCopy copy = new(connection, SqlBulkCopyOptions.KeepIdentity, null) { DestinationTableName = "dbo.Books", BatchSize = 10_000 })
        {
            foreach (DataColumn column in books.Columns)
            {
                copy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
            }
            await copy.WriteToServerAsync(books);
        }
        using (SqlBulkCopy copy = new(connection) { DestinationTableName = "dbo.BookAuthors", BatchSize = 10_000 })
        {
            await copy.WriteToServerAsync(authors);
        }

        Console.WriteLine($"Inserted {books.Rows.Count:N0} books ({curated.Count} curated + {syntheticCount:N0} synthetic) and {authors.Rows.Count:N0} author rows in {watch.Elapsed.TotalSeconds:F1} s");
    }

    // Deterministic synthetic catalog: same seed, same rows on every machine.
    private static IEnumerable<Book> Synthetic(int count)
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
                Isbn: SyntheticIsbn(i),
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
    private static string SyntheticIsbn(int counter)
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
