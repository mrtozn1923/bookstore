using System.Diagnostics;
using static Bookstore.Ordering.Orders.OrderStatus;

namespace Bookstore.Ordering.Orders;

public sealed record Order(string OrderNo, string CustomerEmail, decimal Total, OrderStatus Status);

public static class OrderWorkflow
{
    public static Paid Pay(Pending pending, string paymentId) => new(paymentId);

    public static Shipped Ship(Paid paid, string trackingNo) => new(paid.PaymentId, trackingNo);

    // A shipped order can't be cancelled: only these two overloads exist
    public static Cancelled Cancel(Pending pending, string reason) => new(reason);

    public static Cancelled Cancel(Paid paid, string reason) => new(reason);

    public static string Describe(OrderStatus status) => status switch
    {
        Pending => "pending",
        Paid paid => $"paid with {paid.PaymentId}",
        Shipped shipped => $"shipped with {shipped.TrackingNo}",
        Cancelled cancelled => $"cancelled: {cancelled.Reason}",
        _ => throw new UnreachableException($"Unknown status {status.GetType().Name}")
    };
}
