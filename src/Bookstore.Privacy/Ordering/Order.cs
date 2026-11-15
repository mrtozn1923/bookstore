namespace Bookstore.Ordering;

public sealed class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public DateTime OrderedAtUtc { get; set; }
    public decimal Total { get; set; }
    public required string ShippingAddress { get; set; }
}
