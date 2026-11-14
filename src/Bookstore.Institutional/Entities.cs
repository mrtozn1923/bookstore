namespace Bookstore.Institutional;

public class Institution
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string TaxNumber { get; set; } = "";
    public List<InstitutionUnit> Units { get; set; } = [];
}

public class InstitutionUnit
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public int? ParentId { get; set; }      // null for top-level units
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public InstitutionUnit? Parent { get; set; }
}

public class InstitutionUser
{
    public int Id { get; set; }
    public int InstitutionUnitId { get; set; }
    public int? ApproverId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public DateOnly JoinedOn { get; set; }
    public decimal? SpendingLimit { get; set; }
    public bool IsDeleted { get; set; }
}

public class DiscountAgreement
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public decimal DiscountRate { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public int InstitutionUnitId { get; set; }
    public int PlacedById { get; set; }     // InstitutionUser
    public int Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateTimeOffset CreatedAt { get; set; }
}

public class PurchaseApproval
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ApproverId { get; set; }     // InstitutionUser
    public int Status { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
}

public class Invoice
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public int? OrderId { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public DateOnly IssueDate { get; set; }
    public decimal TotalAmount { get; set; }
}
