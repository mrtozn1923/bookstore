using Bookstore.Catalog;

namespace Bookstore.Ordering;

public sealed record PricedLine(BookInfo Book, int Quantity, decimal LineTotal);

public sealed record CartPrice(IReadOnlyList<PricedLine> Lines, decimal Subtotal);

// Prices cart lines with the current catalog prices; used by the cart view and by order placement.
public static class CartPricing
{
    public static CartPrice Price(IEnumerable<CartItem> items, IReadOnlyDictionary<int, BookInfo> books)
    {
        List<PricedLine> lines =
        [
            .. items
                .Select(i => (Book: books[i.BookId], i.Quantity))
                .OrderBy(x => x.Book.Title)
                .Select(x => new PricedLine(x.Book, x.Quantity, OrderItem.CalculateLineTotal(x.Book.Price, x.Quantity)))
        ];
        return new CartPrice(lines, lines.Sum(l => l.LineTotal));
    }
}
