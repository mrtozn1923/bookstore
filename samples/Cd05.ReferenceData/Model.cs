namespace Bookstore.Institutional.ReferenceData;

// Closed list: drives behavior, changes only with a deploy
public enum OrderStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Rejected = 4,
    Cancelled = 5
}

// The database mirror of the enum in the code
public class OrderStatusRow
{
    public OrderStatus Id { get; set; }
    public string Code { get; set; } = "";
    public string NameTr { get; set; } = "";
    public string NameEn { get; set; } = "";
}

// A list that grows without a deploy: no enum for it in the code
public class BudgetCategory
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string NameTr { get; set; } = "";
    public string NameEn { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Order
{
    public int Id { get; set; }
    public OrderStatus Status { get; set; }
    public int BudgetCategoryId { get; set; }
    public decimal TotalAmount { get; set; }
}

// A closed list of values that carry behavior
public sealed record PaymentTerm(int Id, string Code, bool RequiresDocument, bool IsDeferred)
{
    public static readonly PaymentTerm Card = new(1, "card", RequiresDocument: false, IsDeferred: false);
    public static readonly PaymentTerm Invoice30 = new(2, "invoice-30", RequiresDocument: true, IsDeferred: true);
    public static readonly PaymentTerm BankTransfer = new(3, "bank-transfer", RequiresDocument: false, IsDeferred: false);

    public static IReadOnlyList<PaymentTerm> All { get; } = [Card, Invoice30, BankTransfer];

    public static PaymentTerm FromId(int id) => All.SingleOrDefault(t => t.Id == id)
        ?? throw new ArgumentOutOfRangeException(nameof(id), $"Unknown payment term {id}.");
}

// Naive demo: values not written explicitly
public enum StatusV1 { Draft, PendingApproval, Approved, Rejected }

// Someone added "Submitted" where it looked logical
public enum StatusV2 { Draft, Submitted, PendingApproval, Approved, Rejected }

public enum RenamedStatus { Draft, Approved, Canceled }

public class LegacyOrder
{
    public int Id { get; set; }
    public RenamedStatus Status { get; set; }
}
