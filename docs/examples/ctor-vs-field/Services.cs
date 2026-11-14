public sealed class PrimaryCtorService(IBudgetService budgets)
{
    public bool Check() => budgets is not null;
}

public sealed class FieldService
{
    private readonly IBudgetService budgets;

    public FieldService(IBudgetService budgets) => this.budgets = budgets;

    public bool Check() => budgets is not null;
}
