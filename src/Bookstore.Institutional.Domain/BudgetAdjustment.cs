namespace Bookstore.Institutional.Domain;

public class BudgetAdjustment
{
    private BudgetAdjustment() { } // EF Core için

    public BudgetAdjustment(int unitBudgetId, decimal amount, string reason, int adjustedById, DateTimeOffset adjustedAt)
    {
        UnitBudgetId = unitBudgetId;
        Amount = amount;
        Reason = reason;
        AdjustedById = adjustedById;
        AdjustedAt = adjustedAt;
    }

    public int Id { get; private set; }
    public int UnitBudgetId { get; private set; }
    public decimal Amount { get; private set; }
    public string Reason { get; private set; } = "";
    public int AdjustedById { get; private set; }
    public DateTimeOffset AdjustedAt { get; private set; }
}
