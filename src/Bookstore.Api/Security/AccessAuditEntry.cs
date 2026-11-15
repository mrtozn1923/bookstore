namespace Bookstore.Security;

// One row per access decision on institutional data (ASVS v5.0.0-16.3.2). Never holds order contents.
public sealed class AccessAuditEntry
{
    public long Id { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public required string UserId { get; set; }
    public int CallerInstitutionId { get; set; }
    public required string Action { get; set; }
    public int ResourceInstitutionId { get; set; }
    public string ResourceIds { get; set; } = "";
    public required string Outcome { get; set; }
    public string? TraceId { get; set; }
}
