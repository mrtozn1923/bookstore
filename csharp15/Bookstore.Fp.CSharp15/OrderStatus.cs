namespace Bookstore.Fp.CSharp15;

public sealed record Pending;
public sealed record Paid(string PaymentId);
public sealed record Shipped(string PaymentId, string TrackingNo);
public sealed record Cancelled(string Reason);

public union OrderStatus(Pending, Paid, Shipped, Cancelled);

// Only orders that haven't shipped can be cancelled
public union Cancellable(Pending, Paid);

public static class OrderWorkflow
{
    public static Cancelled Cancel(Cancellable status, string reason) => new(reason);

    public static string Describe(OrderStatus status) => status switch
    {
        Pending => "pending",
        Paid paid => $"paid with {paid.PaymentId}",
        Shipped shipped => $"shipped with {shipped.TrackingNo}",
        Cancelled cancelled => $"cancelled: {cancelled.Reason}"
    };
}
