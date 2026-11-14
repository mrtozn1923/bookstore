using Stateless;

namespace Bookstore.Institutional.StateMachines;

// Superstate için bir değer gerekiyor; ilk altı değer OrderStatus ile aynı, PendingApproval veritabanına hiç yazılmaz
public enum OrderState { Draft, PendingApprover, PendingPurchasingManager, Approved, Rejected, Cancelled, PendingApproval }

public static class OrderStateMachine
{
    public static StateMachine<OrderState, OrderTrigger> Create(OrderRecord r, TransitionContext c, List<string> log)
    {
        StateMachine<OrderState, OrderTrigger> m = new(() => (OrderState)r.Status, s => r.Status = (OrderStatus)s);

        m.Configure(OrderState.Draft)
            .PermitIf(OrderTrigger.Submit, OrderState.PendingApprover, () => c.HasEnoughBudget, "HasEnoughBudget");

        m.Configure(OrderState.PendingApproval)
            .PermitIf(OrderTrigger.Reject, OrderState.Rejected, () => c.HasComment, "HasComment")
            .Permit(OrderTrigger.Cancel, OrderState.Cancelled);

        m.Configure(OrderState.PendingApprover)
            .SubstateOf(OrderState.PendingApproval)
            .OnEntry(() => log.Add("entry: notify approver"))
            // Choice pseudostate: iki guard birbirini dışlamalı
            .PermitIf(OrderTrigger.Approve, OrderState.PendingPurchasingManager,
                () => c.HasEnoughBudget && r.RequiresPurchasingApproval, "RequiresPurchasingApproval")
            .PermitIf(OrderTrigger.Approve, OrderState.Approved,
                () => c.HasEnoughBudget && !r.RequiresPurchasingApproval, "else");

        m.Configure(OrderState.PendingPurchasingManager)
            .SubstateOf(OrderState.PendingApproval)
            .OnEntry(() => log.Add("entry: notify purchasing manager"))
            .PermitIf(OrderTrigger.Approve, OrderState.Approved, () => c.HasEnoughBudget, "HasEnoughBudget");

        m.Configure(OrderState.Approved)
            .OnEntry(() => log.Add("entry: deduct budget, place order at distributor"))
            .PermitIf(OrderTrigger.Cancel, OrderState.Cancelled,
                () => !c.DeliveryDateReached || c.ByPortalAdmin, "BeforeDeliveryOrAdmin");

        m.Configure(OrderState.Cancelled)
            .OnEntry(t =>
            {
                // Transition effect: iade yalnız Approved'dan gelindiyse
                if (t.Source == OrderState.Approved)
                {
                    log.Add("effect: refund budget, cancel distributor order");
                }
            });

        m.OnTransitioned(t => log.Add($"{t.Source} --{t.Trigger}--> {t.Destination}"));
        return m;
    }

    // Tuzak örneği: örtüşen guard'lar (ikisi birden true olabiliyor)
    public static StateMachine<OrderState, OrderTrigger> CreateOverlapping(OrderRecord r)
    {
        StateMachine<OrderState, OrderTrigger> m = new(() => (OrderState)r.Status, s => r.Status = (OrderStatus)s);
        m.Configure(OrderState.PendingApprover)
            .PermitIf(OrderTrigger.Approve, OrderState.PendingPurchasingManager, () => r.Total > 50_000m)
            .PermitIf(OrderTrigger.Approve, OrderState.Approved, () => r.Total >= 50_000m);
        return m;
    }
}
