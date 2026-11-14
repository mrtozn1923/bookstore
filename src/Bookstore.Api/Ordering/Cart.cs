namespace Bookstore.Ordering;

public sealed class Cart
{
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public string? CouponCode { get; set; }
    public List<CartItem> Items { get; } = [];
}

public sealed class CartItem
{
    public int Id { get; set; }
    public Guid CartId { get; set; }
    public int BookId { get; set; }
    public int Quantity { get; set; }
}
