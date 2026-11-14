using Microsoft.Data.SqlClient;

namespace Bookstore.Api;

public interface IBookSource
{
    Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken cancellationToken);
}

// Used when no connection string is configured (dotnet run without a database)
public sealed class InMemoryBookSource : IBookSource
{
    private static readonly Book[] Books =
    [
        new("9780441172719", "Dune", "Frank Herbert", 420.00m),
        new("9789750719387", "İnce Memed", "Yaşar Kemal", 310.00m),
        new("9789750738609", "Çalıkuşu", "Reşat Nuri Güntekin", 240.00m),
        new("9789754700114", "Saatleri Ayarlama Enstitüsü", "Ahmet Hamdi Tanpınar", 280.00m),
        new("9780141439587", "Emma", "Jane Austen", 180.00m),
        new("9789750722752", "Kürk Mantolu Madonna", "Sabahattin Ali", 150.00m),
    ];

    public Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Book>>(Books);
}

// Reads the catalog from BookstoreDb (SQL Server)
public sealed class SqlBookSource(string connectionString) : IBookSource
{
    public async Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken cancellationToken)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using SqlCommand command = new("SELECT Isbn, Title, Author, PriceTry FROM dbo.Books", connection);
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        List<Book> books = [];
        while (await reader.ReadAsync(cancellationToken))
        {
            books.Add(new Book(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetDecimal(3)));
        }
        return books;
    }
}
