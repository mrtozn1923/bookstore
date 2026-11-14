namespace Bookstore.Institutional.Domain;

public class PurchaseApproval
{
    private PurchaseApproval() { } // EF Core için

    internal PurchaseApproval(Order order, InstitutionUser approver, ApprovalStep step, ApprovalDecision decision,
        string? comment, DateTimeOffset decidedAt)
    {
        Order = order;
        Approver = approver;
        ApproverId = approver.Id;
        Step = step;
        Decision = decision;
        Comment = comment;
        DecidedAt = decidedAt;
    }

    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public Order Order { get; private set; } = null!;
    public int ApproverId { get; private set; }
    public InstitutionUser Approver { get; private set; } = null!;
    public ApprovalStep Step { get; private set; }
    public ApprovalDecision Decision { get; private set; }
    public string? Comment { get; private set; }
    public DateTimeOffset DecidedAt { get; private set; }
}
