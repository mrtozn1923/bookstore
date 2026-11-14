namespace Bookstore.Api;

public record BookDto(int Id, string Title, decimal Price);

public record StockDto(int BookId, int Quantity);

public class BookRepository
{
    private int _calls;

    public int Calls => _calls;

    public async Task<BookDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _calls);
        await Task.Delay(500, cancellationToken);
        return id == 1 ? new BookDto(1, "Dune", 450m) : null;
    }
}
