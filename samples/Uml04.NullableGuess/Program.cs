Order order = new();
Console.WriteLine(order.RequiresPurchasingApproval);

public sealed class Order
{
    public const decimal PurchasingApprovalThreshold = 50_000m;
    private readonly List<PurchaseApproval> _approvals = [];

    public int Id { get; private set; }
    public Money Total { get; private set; }
    public string? Note { get; private set; }
    public string RejectionReason { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public int Quantity { get; private set; }
    public bool RequiresPurchasingApproval => Total.Amount > PurchasingApprovalThreshold;
    public IReadOnlyList<PurchaseApproval> Approvals => _approvals;

    internal void Submit(Money total) => Total = total;
}

public readonly record struct Money(decimal Amount, string Currency);
public sealed record PurchaseApproval(int ApproverId, DateTimeOffset DecidedAt);
public enum OrderStatus { Draft, PendingApprover, PendingPurchasingManager, Approved, Rejected, Cancelled }
