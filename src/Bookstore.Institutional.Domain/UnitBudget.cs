namespace Bookstore.Institutional.Domain;

public class UnitBudget
{
    private UnitBudget() { } // EF Core için

    public UnitBudget(int institutionUnitId, int budgetCategoryId, int year, decimal allocatedAmount)
    {
        InstitutionUnitId = institutionUnitId;
        BudgetCategoryId = budgetCategoryId;
        Year = year;
        AllocatedAmount = allocatedAmount;
    }

    public int Id { get; private set; }
    public int InstitutionUnitId { get; private set; }
    public int BudgetCategoryId { get; private set; }
    public int Year { get; private set; }
    public decimal AllocatedAmount { get; private set; }
    public decimal SpentAmount { get; private set; }
    public decimal RemainingAmount => AllocatedAmount - SpentAmount;

    public void Deduct(decimal amount)
    {
        if (amount > RemainingAmount)
            throw new InvalidOperationException($"Only {RemainingAmount} left.");
        SpentAmount += amount;
    }

    public void Refund(decimal amount) => SpentAmount -= amount;
}
