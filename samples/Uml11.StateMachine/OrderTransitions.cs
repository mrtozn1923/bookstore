namespace Bookstore.Institutional.StateMachines;

// Guard'ların okuduğu değerler; varsayılanlar "engel yok" demek
public sealed record TransitionContext(
    bool HasEnoughBudget = true,
    bool RequiresPurchasingApproval = false,
    bool HasComment = false,
    bool DeliveryDateReached = false,
    bool ByPortalAdmin = false);

public static class OrderTransitions
{
    // Diyagramdaki her ok bir satır; satırı olmayan (durum, trigger) çifti geçersiz
    public static OrderStatus? Next(OrderStatus from, OrderTrigger trigger, TransitionContext c) => (from, trigger) switch
    {
        (OrderStatus.Draft, OrderTrigger.Submit) when c.HasEnoughBudget
            => OrderStatus.PendingApprover,

        // Choice pseudostate: [RequiresPurchasingApproval] / [else]
        (OrderStatus.PendingApprover, OrderTrigger.Approve) when c.HasEnoughBudget
            => c.RequiresPurchasingApproval ? OrderStatus.PendingPurchasingManager : OrderStatus.Approved,

        (OrderStatus.PendingPurchasingManager, OrderTrigger.Approve) when c.HasEnoughBudget
            => OrderStatus.Approved,

        // Composite state'in sınırından çıkan oklar: iki alt durum için de geçerli
        (OrderStatus.PendingApprover or OrderStatus.PendingPurchasingManager, OrderTrigger.Reject) when c.HasComment
            => OrderStatus.Rejected,
        (OrderStatus.PendingApprover or OrderStatus.PendingPurchasingManager, OrderTrigger.Cancel)
            => OrderStatus.Cancelled,

        (OrderStatus.Approved, OrderTrigger.Cancel) when !c.DeliveryDateReached || c.ByPortalAdmin
            => OrderStatus.Cancelled,

        _ => null
    };
}

// Örneği kısa tutmak için entity yerine yalnız durum ve tutar taşıyan bir kayıt
public sealed class OrderRecord(int id, decimal total)
{
    public int Id { get; } = id;
    public decimal Total { get; } = total;
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public bool RequiresPurchasingApproval => Total > 50_000m;

    public void Fire(OrderTrigger trigger, TransitionContext context) =>
        Status = OrderTransitions.Next(Status, trigger, context)
            ?? throw new InvalidOperationException($"'{trigger}' is not allowed when the order is {Status}.");
}
