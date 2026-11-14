using System.Collections.Concurrent;

namespace Bookstore.Api.Carts;

public record CartItem(string Isbn, int Quantity);

public class CartStore
{
    private readonly ConcurrentDictionary<Guid, List<CartItem>> _carts = new();

    public IReadOnlyList<CartItem> Get(Guid cartId) =>
        _carts.TryGetValue(cartId, out List<CartItem>? items) ? items : [];

    public IReadOnlyList<CartItem> Add(Guid cartId, CartItem item)
    {
        List<CartItem> items = _carts.GetOrAdd(cartId, _ => []);
        lock (items)
        {
            items.Add(item);
            return items.ToList();
        }
    }
}
