InstitutionUser ada = new(1001, "S-1001", "Ada Lovelace", ApproverId: 420, InstitutionUnitId: 7);
BudgetCategory classroomSets = new("classroom-sets", "Classroom Sets", DeductsFromBudget: true);
UnitBudget budget = new(ada.InstitutionUnitId, classroomSets.Code, Year: 2026, AllocatedAmount: 120_000m, SpentAmount: 45_000m);
Order order = new(42, ada.Id, classroomSets.Code, BookId: 1, Quantity: 150, new DateOnly(2026, 12, 21), Total: 56_700m);

Console.WriteLine($"Remaining: {budget.RemainingAmount}, order total: {order.Total}");
Console.WriteLine($"Enough budget (R3): {budget.RemainingAmount >= order.Total}");
Console.WriteLine($"Needs purchasing manager (R7): {order.RequiresPurchasingApproval}");

// R1, R6, R7: kullanıcı, onaycısı ve birimi
public sealed record InstitutionUser(int Id, string StaffNumber, string FullName, int? ApproverId, int InstitutionUnitId);

// R2, R3: bütçe kalemi; bağış bütçeden düşmez
public sealed record BudgetCategory(string Code, string Name, bool DeductsFromBudget);

// R3, R9, R12: birimin yıllık bütçesi; kalan tutar saklanmaz, hesaplanır
public sealed record UnitBudget(int InstitutionUnitId, string BudgetCategoryCode, int Year, decimal AllocatedAmount, decimal SpentAmount)
{
    public decimal RemainingAmount => AllocatedAmount - SpentAmount;
}

// R1, R2, R7: sipariş; eşik requirement'taki "50.000 TL"
public sealed record Order(int Id, int PlacedById, string BudgetCategoryCode, int BookId, int Quantity, DateOnly DeliveryDate, decimal Total)
{
    public const decimal PurchasingApprovalThreshold = 50_000m;
    public bool RequiresPurchasingApproval => Total > PurchasingApprovalThreshold;
}
