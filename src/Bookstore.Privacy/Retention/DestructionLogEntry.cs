using Microsoft.Extensions.Compliance.Classification;

namespace Bookstore.Privacy.Retention;

// Record of every destruction run (kept separately from the data it describes; holds no personal data).
[DataCategory("privacy-request")]
public sealed class DestructionLogEntry
{
    [NoDataClassification]
    public int Id { get; set; }

    [NoDataClassification]
    public DateTime ExecutedAtUtc { get; set; }

    [NoDataClassification]
    public required string Category { get; set; }

    [NoDataClassification]
    public required string Method { get; set; }

    [NoDataClassification]
    public int RecordCount { get; set; }

    [NoDataClassification]
    public required string Reason { get; set; }
}
