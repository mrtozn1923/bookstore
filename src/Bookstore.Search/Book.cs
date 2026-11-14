using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bookstore.Search;

public sealed record BookAuthor(string Name, string Role);

public sealed record Book(
    string Isbn,
    string Title,
    IReadOnlyList<BookAuthor> Authors,
    IReadOnlyList<string> Categories,
    string Language,
    decimal Price,
    string Currency,
    int StockQuantity,
    DateOnly PublishedOn,
    decimal? AverageRating,
    string Description);

public static class BookCatalog
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task<IReadOnlyList<Book>> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        await using FileStream stream = File.OpenRead(path);
        List<Book>? books = await JsonSerializer.DeserializeAsync<List<Book>>(stream, JsonOptions, cancellationToken);
        return books ?? [];
    }
}
