namespace Bookstore.Institutional;

public sealed class Institution
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string BillingEmail { get; set; }
}

// A campus, library or department of an institution. Requests above the limit need an approver.
public sealed class InstitutionUnit
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public required string Name { get; set; }
    public decimal ApprovalLimit { get; set; }
}
