namespace Bookstore.Ordering;

public enum CouponKind
{
    Percent,
    Amount
}

public sealed class Coupon
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public CouponKind Kind { get; set; }
    public decimal Value { get; set; }
    public decimal MinSubtotal { get; set; }

    // null: the coupon applies to every book; otherwise only to books in this category.
    public int? CategoryId { get; set; }
    public DateTime ExpiresAtUtc { get; set; }

    // Written by the marketing team in the back office and shown in the cart.
    public string Description { get; set; } = "";
}
