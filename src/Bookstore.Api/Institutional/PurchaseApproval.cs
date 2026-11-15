namespace Bookstore.Api.Institutional;

public sealed class PurchaseApproval
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public required string InstitutionName { get; set; }
    public required string ApproverName { get; set; }
    public required string ApproverEmail { get; set; }
    public required string ApproverCulture { get; set; }
    public required string RequestedBy { get; set; }
    public required string UnitName { get; set; }
    public decimal Total { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public string? Note { get; set; }
    public bool IsDecided { get; set; }
    public DateTime? LastRemindedAtUtc { get; set; }
}
