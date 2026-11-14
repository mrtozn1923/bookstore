using System.Data;
using System.Diagnostics;
using Bookstore.Search;
using Microsoft.Data.SqlClient;

namespace Bookstore.SearchLab;

// Creates dbo.Books / dbo.BookAuthors and fills them: data/books.json plus synthetic rows.
public static class SqlSeeder
{
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
        List<Book> all = [.. curated, .. SyntheticBooks.Generate(syntheticCount)];

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
}
