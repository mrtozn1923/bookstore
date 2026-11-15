using Bookstore.Web.Catalog;

namespace Bookstore.Web.Ordering;

public sealed class CartService(IBookCatalog catalog)
{
    // Checks the business rules for "add to cart"; where the cart itself lives is the next post's topic.
    public async Task<Book?> AddAsync(int bookId, int quantity, CancellationToken cancellationToken)
    {
        Book? book = await catalog.FindAsync(bookId, cancellationToken);
        if (book is null)
        {
            return null;
        }

        if (book.Stock < quantity)
        {
            throw new OutOfStockException(book.Title);
        }

        return book;
    }
}
