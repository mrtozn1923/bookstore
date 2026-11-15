namespace Bookstore.Fp.CSharp15.Closed;

// Only this assembly can add direct cases; closed implies abstract
public closed record OrderStatus;

public sealed record Pending : OrderStatus;
public sealed record Paid(string PaymentId) : OrderStatus;
public sealed record Shipped(string PaymentId, string TrackingNo) : OrderStatus;
public sealed record Cancelled(string Reason) : OrderStatus;

public static class OrderWorkflow
{
    public static string Describe(OrderStatus status) => status switch
    {
        Pending => "pending",
        Paid paid => $"paid with {paid.PaymentId}",
        Shipped shipped => $"shipped with {shipped.TrackingNo}",
        Cancelled cancelled => $"cancelled: {cancelled.Reason}"
    };
}
