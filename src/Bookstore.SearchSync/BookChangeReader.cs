using System.Buffers.Binary;
using Bookstore.Search;
using Microsoft.Data.SqlClient;

namespace Bookstore.SearchSync;

public sealed record BookChange(Book Book, bool IsDiscontinued, long Version);

public sealed record ChangeBatch(IReadOnlyList<BookChange> Changes, byte[] LastRowVer);

// Reads changed books in RowVer order, never past rows that an open transaction may still commit.
public sealed class BookChangeReader(string connectionString)
{
    private const string ChangesSql = """
        DECLARE @upper binary(8) = MIN_ACTIVE_ROWVERSION();
        SELECT TOP (@batchSize) BookId, Isbn, Title, Language, Categories, Price, StockQuantity, PublishedOn,
               AverageRating, Description, IsDiscontinued, RowVer
        FROM dbo.Books
        WHERE RowVer > @last AND RowVer < @upper
        ORDER BY RowVer;
        """;

    private const string AuthorsSql = """
        SELECT a.BookId, a.Name, a.Role
        FROM dbo.BookAuthors AS a
        JOIN OPENJSON(@ids) WITH (BookId int '$') AS ids ON ids.BookId = a.BookId
        ORDER BY a.BookId, a.Position;
        """;

    public async Task<byte[]> GetCheckpointAsync(string name, CancellationToken cancellationToken)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using SqlCommand command = new("SELECT LastRowVer FROM dbo.SearchSyncState WHERE Name = @name;", connection);
        command.Parameters.AddWithValue("@name", name);
        object? value = await command.ExecuteScalarAsync(cancellationToken);
        return value as byte[] ?? new byte[8];
    }

    public async Task SaveCheckpointAsync(string name, byte[] lastRowVer, CancellationToken cancellationToken)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using SqlCommand command = new("""
            MERGE dbo.SearchSyncState AS target
            USING (SELECT @name AS Name) AS source ON target.Name = source.Name
            WHEN MATCHED THEN UPDATE SET LastRowVer = @last, UpdatedAt = SYSUTCDATETIME()
            WHEN NOT MATCHED THEN INSERT (Name, LastRowVer, UpdatedAt) VALUES (@name, @last, SYSUTCDATETIME());
            """, connection);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.Add("@last", System.Data.SqlDbType.Binary, 8).Value = lastRowVer;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<ChangeBatch> ReadChangesAsync(byte[] lastRowVer, int batchSize, CancellationToken cancellationToken)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken);

        List<(int BookId, BookRow Row)> rows = [];
        await using (SqlCommand command = new(ChangesSql, connection))
        {
            command.Parameters.AddWithValue("@batchSize", batchSize);
            command.Parameters.Add("@last", System.Data.SqlDbType.Binary, 8).Value = lastRowVer;
            await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add((reader.GetInt32(0), new BookRow(
                    Isbn: reader.GetString(1),
                    Title: reader.GetString(2),
                    Language: reader.GetString(3),
                    Categories: reader.GetString(4).Split(',', StringSplitOptions.RemoveEmptyEntries),
                    Price: reader.GetDecimal(5),
                    StockQuantity: reader.GetInt32(6),
                    PublishedOn: DateOnly.FromDateTime(reader.GetDateTime(7)),
                    AverageRating: reader.IsDBNull(8) ? null : reader.GetDecimal(8),
                    Description: reader.GetString(9),
                    IsDiscontinued: reader.GetBoolean(10),
                    RowVer: (byte[])reader[11])));
            }
        }

        if (rows.Count == 0)
        {
            return new ChangeBatch([], lastRowVer);
        }

        Dictionary<int, List<BookAuthor>> authors = [];
        await using (SqlCommand command = new(AuthorsSql, connection))
        {
            command.Parameters.AddWithValue("@ids", $"[{string.Join(',', rows.Select(r => r.BookId))}]");
            await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                int bookId = reader.GetInt32(0);
                if (!authors.TryGetValue(bookId, out List<BookAuthor>? list))
                {
                    list = [];
                    authors[bookId] = list;
                }
                list.Add(new BookAuthor(reader.GetString(1), reader.GetString(2)));
            }
        }

        List<BookChange> changes = rows.Select(r => new BookChange(
            new Book(r.Row.Isbn, r.Row.Title, authors.GetValueOrDefault(r.BookId) ?? [], r.Row.Categories, r.Row.Language,
                r.Row.Price, "TRY", r.Row.StockQuantity, r.Row.PublishedOn, r.Row.AverageRating, r.Row.Description),
            r.Row.IsDiscontinued,
            // rowversion is an 8-byte big-endian counter: a natural external version for Elasticsearch.
            BinaryPrimitives.ReadInt64BigEndian(r.Row.RowVer))).ToList();

        return new ChangeBatch(changes, rows[^1].Row.RowVer);
    }

    private sealed record BookRow(string Isbn, string Title, string Language, string[] Categories, decimal Price,
        int StockQuantity, DateOnly PublishedOn, decimal? AverageRating, string Description, bool IsDiscontinued, byte[] RowVer);
}
