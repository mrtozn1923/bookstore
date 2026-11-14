namespace Bookstore.Institutional.StateMachines.Tests;

public class OrderTransitionsTests
{
    // Diyagramdaki her ok bir test satırı
    public static TheoryData<OrderStatus, OrderTrigger, TransitionContext, OrderStatus> Arrows => new()
    {
        { OrderStatus.Draft, OrderTrigger.Submit, new(), OrderStatus.PendingApprover },
        { OrderStatus.PendingApprover, OrderTrigger.Approve, new(RequiresPurchasingApproval: true), OrderStatus.PendingPurchasingManager },
        { OrderStatus.PendingApprover, OrderTrigger.Approve, new(RequiresPurchasingApproval: false), OrderStatus.Approved },
        { OrderStatus.PendingPurchasingManager, OrderTrigger.Approve, new(), OrderStatus.Approved },
        { OrderStatus.PendingApprover, OrderTrigger.Reject, new(HasComment: true), OrderStatus.Rejected },
        { OrderStatus.PendingPurchasingManager, OrderTrigger.Reject, new(HasComment: true), OrderStatus.Rejected },
        { OrderStatus.PendingApprover, OrderTrigger.Cancel, new(), OrderStatus.Cancelled },
        { OrderStatus.PendingPurchasingManager, OrderTrigger.Cancel, new(), OrderStatus.Cancelled },
        { OrderStatus.Approved, OrderTrigger.Cancel, new(DeliveryDateReached: false), OrderStatus.Cancelled },
        { OrderStatus.Approved, OrderTrigger.Cancel, new(DeliveryDateReached: true, ByPortalAdmin: true), OrderStatus.Cancelled },
    };

    // Guard kapalıyken ok ateşlenmez
    public static TheoryData<OrderStatus, OrderTrigger, TransitionContext> ClosedGuards => new()
    {
        { OrderStatus.Draft, OrderTrigger.Submit, new(HasEnoughBudget: false) },
        { OrderStatus.PendingApprover, OrderTrigger.Approve, new(HasEnoughBudget: false) },
        { OrderStatus.PendingPurchasingManager, OrderTrigger.Approve, new(HasEnoughBudget: false) },
        { OrderStatus.PendingApprover, OrderTrigger.Reject, new(HasComment: false) },
        { OrderStatus.Approved, OrderTrigger.Cancel, new(DeliveryDateReached: true, ByPortalAdmin: false) },
    };

    // Diyagramda oku olmayan her (durum, trigger) çifti: 6 x 4 = 24 hücreden okların kapsamadığı 16 hücre
    public static TheoryData<OrderStatus, OrderTrigger> MissingArrows()
    {
        TheoryData<OrderStatus, OrderTrigger> data = [];
        TransitionContext open = new(HasComment: true, ByPortalAdmin: true);
        foreach (OrderStatus s in Enum.GetValues<OrderStatus>())
        {
            foreach (OrderTrigger t in Enum.GetValues<OrderTrigger>())
            {
                if (OrderTransitions.Next(s, t, open) is null)
                {
                    data.Add(s, t);
                }
            }
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(Arrows))]
    public void Arrow_leads_to_target(OrderStatus from, OrderTrigger trigger, TransitionContext context, OrderStatus expected) =>
        Assert.Equal(expected, OrderTransitions.Next(from, trigger, context));

    [Theory]
    [MemberData(nameof(ClosedGuards))]
    public void Closed_guard_blocks_arrow(OrderStatus from, OrderTrigger trigger, TransitionContext context) =>
        Assert.Null(OrderTransitions.Next(from, trigger, context));

    [Theory]
    [MemberData(nameof(MissingArrows))]
    public void Missing_arrow_is_rejected(OrderStatus from, OrderTrigger trigger) =>
        Assert.Null(OrderTransitions.Next(from, trigger, new(HasComment: true, ByPortalAdmin: true)));

    [Fact]
    public void Diagram_has_exactly_eight_open_cells()
    {
        TransitionContext open = new(HasComment: true, ByPortalAdmin: true);
        int openCells = Enum.GetValues<OrderStatus>()
            .SelectMany(s => Enum.GetValues<OrderTrigger>().Select(t => OrderTransitions.Next(s, t, open)))
            .Count(next => next is not null);
        Assert.Equal(8, openCells);
    }
}
