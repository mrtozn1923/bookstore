Order order = new(42, placedById: 1001, budgetCategoryId: 2, bookId: 1, quantity: 150,
    deliveryDate: new DateOnly(2026, 12, 21), note: null);
order.Submit(total: new Money(56_700m, "TRY"), now: new DateTimeOffset(2026, 12, 1, 9, 0, 0, TimeSpan.FromHours(3)));

UnitBudget budget = new(institutionUnitId: 7, budgetCategoryId: 2, year: 2026, allocatedAmount: 120_000m);
budget.Deduct(45_000m);

Console.WriteLine($"{order.Status}, needs purchasing manager: {order.RequiresPurchasingApproval}");
Console.WriteLine($"Two orders: {order.Total.Add(new Money(6_840m, "TRY"))}");
Console.WriteLine($"Remaining: {budget.RemainingAmount}");
Console.WriteLine(order.Total == new Money(56_700m, "TRY"));

// «enumeration»
public enum OrderStatus { Draft, PendingApprover, PendingPurchasingManager, Approved, Rejected, Cancelled }

// «dataType»: kimliği yok, iki değeri de eşitse eşittir
public readonly record struct Money(decimal Amount, string Currency)
{
    public Money Add(Money other) => other.Currency == Currency
        ? new Money(Amount + other.Amount, Currency)
        : throw new InvalidOperationException($"Cannot add {other.Currency} to {Currency}.");
}

// «interface»: operation'lar abstract
public interface IOrderRepository
{
    Task<Order?> FindAsync(int id, CancellationToken ct);
    void Add(Order order);
}

public sealed record PurchaseApproval(int ApproverId, DateTimeOffset DecidedAt);

public sealed class Order(int id, int placedById, int budgetCategoryId, int bookId, int quantity,
    DateOnly deliveryDate, string? note)
{
    // + purchasingApprovalThreshold: decimal = 50000 {readOnly}, altı çizili (static)
    public const decimal PurchasingApprovalThreshold = 50_000m;

    // - approvals: PurchaseApproval [0..*] {ordered}
    private readonly List<PurchaseApproval> _approvals = [];

    public int Id { get; } = id;                                        // + id: int {readOnly}
    public int PlacedById { get; } = placedById;
    public int BudgetCategoryId { get; } = budgetCategoryId;
    public int BookId { get; } = bookId;
    public int Quantity { get; private set; } = quantity;               // + quantity: int
    public DateOnly DeliveryDate { get; private set; } = deliveryDate;  // + deliveryDate: DateOnly
    public string? Note { get; private set; } = note;                   // + note: string [0..1]
    public Money Total { get; private set; } = new(0m, "TRY");          // + total: Money
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;  // + status: OrderStatus = Draft
    public DateTimeOffset? SubmittedAt { get; private set; }            // + submittedAt: DateTimeOffset [0..1]
    public bool RequiresPurchasingApproval => Total.Amount > PurchasingApprovalThreshold;  // + /requiresPurchasingApproval: bool
    public IReadOnlyList<PurchaseApproval> Approvals => _approvals;

    // + submit(total: Money, now: DateTimeOffset)
    public void Submit(Money total, DateTimeOffset now)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException($"Order {Id} is already {Status}.");
        Total = total;
        SubmittedAt = now;
        Status = OrderStatus.PendingApprover;
    }

    // + cancel(today: DateOnly)
    public void Cancel(DateOnly today)
    {
        bool cancellable = Status is OrderStatus.PendingApprover or OrderStatus.PendingPurchasingManager
            || (Status == OrderStatus.Approved && today < DeliveryDate);
        if (!cancellable)
            throw new InvalidOperationException($"Order {Id} cannot be cancelled in status {Status}.");
        Status = OrderStatus.Cancelled;
    }
}

public sealed class UnitBudget(int institutionUnitId, int budgetCategoryId, int year, decimal allocatedAmount)
{
    public int InstitutionUnitId { get; } = institutionUnitId;
    public int BudgetCategoryId { get; } = budgetCategoryId;
    public int Year { get; } = year;
    public decimal AllocatedAmount { get; } = allocatedAmount;
    public decimal SpentAmount { get; private set; }
    public decimal RemainingAmount => AllocatedAmount - SpentAmount;    // + /remainingAmount: decimal

    public void Deduct(decimal amount)
    {
        if (amount > RemainingAmount)
            throw new InvalidOperationException($"Only {RemainingAmount} left.");
        SpentAmount += amount;
    }
}
