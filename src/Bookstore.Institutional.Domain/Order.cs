namespace Bookstore.Institutional.Domain;

public class Order
{
    public const decimal PurchasingApprovalThreshold = 50_000m;
    public const int MinimumQuantity = 20;

    private readonly List<PurchaseApproval> approvals = [];

    private Order() { } // EF Core için

    public Order(InstitutionUser placedBy, BudgetCategory budgetCategory, int bookId, int quantity,
        DateOnly deliveryDate, string? note, DateTimeOffset now,
        ShipmentPreference shipmentPreference = ShipmentPreference.SingleShipment, int? minFirstShipment = null)
    {
        // R2: kurumsal siparişte en az 20 adet
        if (quantity < MinimumQuantity)
            throw new OrderRuleException("minimum-quantity", $"An institutional order needs at least {MinimumQuantity} copies.");
        // R13: ilk sevkiyat alt sınırı yalnız kısmi sevkiyatta verilir ve sipariş adedini aşamaz
        if (minFirstShipment is int min &&
            (shipmentPreference != ShipmentPreference.PartialAllowed || min < 1 || min > quantity))
            throw new OrderRuleException("min-first-shipment",
                "A minimum first shipment needs partial shipment and must be between 1 and the order quantity.");
        PlacedBy = placedBy;
        PlacedById = placedBy.Id;
        BudgetCategory = budgetCategory;
        BudgetCategoryId = budgetCategory.Id;
        BookId = bookId;
        Quantity = quantity;
        DeliveryDate = deliveryDate;
        Note = note;
        ShipmentPreference = shipmentPreference;
        MinFirstShipment = minFirstShipment;
        Total = Money.Try(0m);
        Status = OrderStatus.Draft;
        CreatedAt = now;
    }

    public int Id { get; private set; }
    public int PlacedById { get; private set; }
    public InstitutionUser PlacedBy { get; private set; } = null!;
    public int BudgetCategoryId { get; private set; }
    public BudgetCategory BudgetCategory { get; private set; } = null!;
    public int BookId { get; private set; }
    public int Quantity { get; private set; }
    public DateOnly DeliveryDate { get; private set; }
    public Money Total { get; private set; }
    public string? Note { get; private set; }
    public ShipmentPreference ShipmentPreference { get; private set; }
    public int? MinFirstShipment { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public OrderAttachment? Attachment { get; private set; }
    public IReadOnlyList<PurchaseApproval> Approvals => approvals;

    public bool RequiresPurchasingApproval => Total.Amount > PurchasingApprovalThreshold;

    // Draft --submit--> PendingApprover
    public void Submit(Money total, DateTimeOffset now)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOrderTransitionException(Status, "submit");
        Total = total;
        SubmittedAt = now;
        Status = OrderStatus.PendingApprover;
    }

    // PendingApprover --approve [RequiresPurchasingApproval]--> PendingPurchasingManager
    // PendingApprover --approve [else]--> Approved
    // PendingPurchasingManager --approve--> Approved
    public void Approve(InstitutionUser approver, string? comment, DateTimeOffset now)
    {
        ApprovalStep step = CurrentStep("approve");
        approvals.Add(new PurchaseApproval(this, approver, step, ApprovalDecision.Approved, comment, now));
        Status = step == ApprovalStep.Approver && RequiresPurchasingApproval
            ? OrderStatus.PendingPurchasingManager
            : OrderStatus.Approved;
    }

    public void Reject(InstitutionUser approver, string comment, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(comment))
            throw new ArgumentException("A rejection needs a reason.", nameof(comment));
        approvals.Add(new PurchaseApproval(this, approver, CurrentStep("reject"), ApprovalDecision.Rejected, comment, now));
        Status = OrderStatus.Rejected;
    }

    public void Cancel(DateOnly today)
    {
        bool cancellable = Status is OrderStatus.PendingApprover or OrderStatus.PendingPurchasingManager
            || (Status == OrderStatus.Approved && today < DeliveryDate);
        if (!cancellable)
            throw new InvalidOrderTransitionException(Status, "cancel");
        Status = OrderStatus.Cancelled;
    }

    public void Attach(OrderAttachment attachment) => Attachment = attachment;

    private ApprovalStep CurrentStep(string trigger) => Status switch
    {
        OrderStatus.PendingApprover => ApprovalStep.Approver,
        OrderStatus.PendingPurchasingManager => ApprovalStep.PurchasingManager,
        _ => throw new InvalidOrderTransitionException(Status, trigger)
    };
}
