namespace Bookstore.Catalog;

public sealed class BookService(ILogger<BookService> logger, IBookCache cache, IBookRepository books)
{
    public async Task<Book?> FindAsync(string isbn, CancellationToken ct)
    {
        string key = $"book:{isbn}";
        if (cache.TryGet(key, out Book? book))
            return book;

        logger.LogDebug($"Cache miss for {isbn}");
        book = await books.FindAsync(isbn, ct);
        if (book is { Stock: 0 })
            logger.LogWarning($"Book {isbn} is out of stock");
        return book;
    }
}
