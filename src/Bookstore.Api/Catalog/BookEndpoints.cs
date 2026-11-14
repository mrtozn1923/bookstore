using Microsoft.Data.SqlClient;

namespace Bookstore.Api.Catalog;

public static class BookEndpoints
{
    public static void MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        // Connection string her istekte configuration'dan okunur: yenilenen credential hemen kullanılır
        app.MapGet("/api/books", async (IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            await using var connection = new SqlConnection(configuration.GetConnectionString("Bookstore"));
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(
                "SELECT SUSER_SNAME(), COUNT(*) FROM dbo.Books", connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);

            return new { DbUser = reader.GetString(0), Books = reader.GetInt32(1) };
        });
    }
}
