using Microsoft.Extensions.Compliance.Classification;

namespace Bookstore.Privacy.Consent;

// Append-only: every decision (grant or withdrawal) is a new row; the current state is the latest row per purpose.
[DataCategory("consent")]
public sealed class ConsentRecord
{
    [NoDataClassification]
    public long Id { get; set; }

    // A signed-in customer, or an anonymous visitor known only by the random id in the consent cookie.
    [PersonalData(InformationType = "Customer ID")]
    public int? CustomerId { get; set; }

    [PersonalData(InformationType = "Device ID")]
    public Guid? VisitorId { get; set; }

    [NoDataClassification]
    public required string Purpose { get; set; }

    [NoDataClassification]
    public bool Granted { get; set; }

    // Which privacy notice the person saw and which consent text they answered.
    [NoDataClassification]
    public required string NoticeVersion { get; set; }

    [NoDataClassification]
    public required string Source { get; set; }

    [NoDataClassification]
    public DateTime RecordedAtUtc { get; set; }
}
