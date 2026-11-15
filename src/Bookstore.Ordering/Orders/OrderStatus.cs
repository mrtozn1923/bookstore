using System.Text.Json.Serialization;

namespace Bookstore.Ordering.Orders;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(Pending), "pending")]
[JsonDerivedType(typeof(Paid), "paid")]
[JsonDerivedType(typeof(Shipped), "shipped")]
[JsonDerivedType(typeof(Cancelled), "cancelled")]
public abstract record OrderStatus
{
    // Only the nested cases below can derive from OrderStatus
    private OrderStatus() { }

    public sealed record Pending : OrderStatus;
    public sealed record Paid(string PaymentId) : OrderStatus;
    public sealed record Shipped(string PaymentId, string TrackingNo) : OrderStatus;
    public sealed record Cancelled(string Reason) : OrderStatus;
}
