using Bookstore.Web.Catalog;

namespace Bookstore.Web.Ordering;

public sealed record CartItemView(Book Book, int Quantity)
{
    public decimal Total => Book.Price * Quantity;
}

public sealed class CartService(IBookCatalog catalog, ICartStore store, IHttpContextAccessor accessor)
{
    private HttpContext Context => accessor.HttpContext ?? throw new InvalidOperationException("No HTTP request.");

    public async Task<Book?> AddAsync(int bookId, int quantity, CancellationToken cancellationToken)
    {
        Book? book = await catalog.FindAsync(bookId, cancellationToken);
        if (book is null)
        {
            return null;
        }

        List<CartLine> lines = [.. store.Read(Context)];
        int inCart = lines.Where(l => l.BookId == bookId).Sum(l => l.Quantity);
        if (book.Stock < inCart + quantity)
        {
            throw new OutOfStockException(book.Title);
        }

        lines.RemoveAll(l => l.BookId == bookId);
        lines.Add(new CartLine(bookId, inCart + quantity));
        store.Write(Context, lines);
        return book;
    }

    public async Task<IReadOnlyList<CartItemView>> GetItemsAsync(CancellationToken cancellationToken)
    {
        List<CartItemView> items = [];
        foreach (CartLine line in store.Read(Context))
        {
            // Books removed from the catalog silently drop out of the cart
            if (await catalog.FindAsync(line.BookId, cancellationToken) is { } book)
            {
                items.Add(new CartItemView(book, line.Quantity));
            }
        }

        return items;
    }
}
