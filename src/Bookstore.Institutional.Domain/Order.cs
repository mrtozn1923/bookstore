namespace Bookstore.Institutional.Domain;

public class Order
{
    public const decimal PurchasingApprovalThreshold = 50_000m;

    private readonly List<PurchaseApproval> approvals = [];

    private Order() { } // EF Core için

    public Order(InstitutionUser placedBy, BudgetCategory budgetCategory, int bookId, int quantity,
        DateOnly deliveryDate, string? note, DateTimeOffset now)
    {
        PlacedBy = placedBy;
        PlacedById = placedBy.Id;
        BudgetCategory = budgetCategory;
        BudgetCategoryId = budgetCategory.Id;
        BookId = bookId;
        Quantity = quantity;
        DeliveryDate = deliveryDate;
        Note = note;
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
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public OrderAttachment? Attachment { get; private set; }
    public IReadOnlyList<PurchaseApproval> Approvals => approvals;

    public bool RequiresPurchasingApproval => Total.Amount > PurchasingApprovalThreshold;

    public void Submit(Money total, DateTimeOffset now)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException($"Order {Id} is already {Status}.");
        Total = total;
        SubmittedAt = now;
        Status = OrderStatus.PendingApprover;
    }

    public void Approve(InstitutionUser approver, string? comment, DateTimeOffset now)
    {
        ApprovalStep step = CurrentStep();
        approvals.Add(new PurchaseApproval(this, approver, step, ApprovalDecision.Approved, comment, now));
        Status = step == ApprovalStep.Approver && RequiresPurchasingApproval
            ? OrderStatus.PendingPurchasingManager
            : OrderStatus.Approved;
    }

    public void Reject(InstitutionUser approver, string comment, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(comment))
            throw new ArgumentException("A rejection needs a reason.", nameof(comment));
        approvals.Add(new PurchaseApproval(this, approver, CurrentStep(), ApprovalDecision.Rejected, comment, now));
        Status = OrderStatus.Rejected;
    }

    public void Cancel(DateOnly today)
    {
        bool cancellable = Status is OrderStatus.PendingApprover or OrderStatus.PendingPurchasingManager
            || (Status == OrderStatus.Approved && today < DeliveryDate);
        if (!cancellable)
            throw new InvalidOperationException($"Order {Id} cannot be cancelled in status {Status}.");
        Status = OrderStatus.Cancelled;
    }

    public void Attach(OrderAttachment attachment) => Attachment = attachment;

    private ApprovalStep CurrentStep() => Status switch
    {
        OrderStatus.PendingApprover => ApprovalStep.Approver,
        OrderStatus.PendingPurchasingManager => ApprovalStep.PurchasingManager,
        _ => throw new InvalidOperationException($"Order {Id} is not waiting for a decision.")
    };
}
