using Bookstore.Data;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Ordering;

// The only door other modules use to change a cart.
public interface ICartService
{
    // Adds one copy to the cart (up to 99). It does not save: the caller saves once, so both changes commit together.
    Task<bool> TryAddOneAsync(Guid cartId, int bookId, CancellationToken ct);
}

internal sealed class CartService(BookstoreContext db, TimeProvider clock) : ICartService
{
    private const int MaxQuantity = 99;   // same limit as SetQuantityRequest

    public async Task<bool> TryAddOneAsync(Guid cartId, int bookId, CancellationToken ct)
    {
        Cart? cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == cartId, ct);
        if (cart is null)
        {
            return false;
        }

        CartItem? item = cart.Items.FirstOrDefault(i => i.BookId == bookId);
        if (item is null)
        {
            cart.Items.Add(new CartItem { BookId = bookId, Quantity = 1 });
        }
        else
        {
            item.Quantity = Math.Min(item.Quantity + 1, MaxQuantity);
        }
        cart.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
        return true;
    }
}
