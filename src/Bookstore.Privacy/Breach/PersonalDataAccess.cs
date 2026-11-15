using Microsoft.Extensions.Compliance.Classification;

namespace Bookstore.Privacy.Breach;

// Who read whose personal data, and which category. Written by support, export and back office endpoints.
[DataCategory("access-audit")]
public sealed class PersonalDataAccess
{
    [NoDataClassification]
    public long Id { get; set; }

    [PersonalData(InformationType = "Staff ID")]
    public required string ActorId { get; set; }

    [PersonalData(InformationType = "Customer ID")]
    public int CustomerId { get; set; }

    [NoDataClassification]
    public required string DataCategory { get; set; }

    [NoDataClassification]
    public required string Action { get; set; }

    [NoDataClassification]
    public DateTime AtUtc { get; set; }
}
