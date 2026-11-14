namespace Bookstore.Institutional.Domain;

public class BudgetCategory
{
    private BudgetCategory() { } // EF Core için

    public BudgetCategory(string code, string name, bool deductsFromBudget)
    {
        Code = code;
        Name = name;
        DeductsFromBudget = deductsFromBudget;
        IsActive = true;
    }

    public int Id { get; private set; }
    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";
    public bool DeductsFromBudget { get; private set; }
    public bool IsActive { get; private set; }

    public void Deactivate() => IsActive = false;
}
