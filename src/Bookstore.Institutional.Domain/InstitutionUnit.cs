namespace Bookstore.Institutional.Domain;

public class InstitutionUnit
{
    private InstitutionUnit() { } // EF Core için

    public InstitutionUnit(string name, InstitutionUser? purchasingManager)
    {
        Name = name;
        PurchasingManager = purchasingManager;
        PurchasingManagerId = purchasingManager?.Id;
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = "";
    public int? PurchasingManagerId { get; private set; }
    public InstitutionUser? PurchasingManager { get; private set; }
}
