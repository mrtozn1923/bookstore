namespace Bookstore.Wishlists;

public sealed class Wishlist
{
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<WishlistItem> Items { get; } = [];
}

public sealed class WishlistItem
{
    public int Id { get; set; }
    public Guid WishlistId { get; set; }
    public int BookId { get; set; }
    public DateTime AddedAtUtc { get; set; }
}
