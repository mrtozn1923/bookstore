using Microsoft.Extensions.Compliance.Classification;

namespace Bookstore.Privacy;

[DataCategory("privacy-request")]
public sealed class ErasureRequest
{
    [NoDataClassification]
    public int Id { get; set; }

    [PersonalData(InformationType = "Customer ID")]
    public int CustomerId { get; set; }

    [NoDataClassification]
    public DateTime RequestedAtUtc { get; set; }

    [NoDataClassification]
    public DateTime DueAtUtc { get; set; }

    [NoDataClassification]
    public DateTime? CompletedAtUtc { get; set; }
}
