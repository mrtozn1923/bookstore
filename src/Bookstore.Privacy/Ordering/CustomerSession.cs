using Bookstore.Privacy;
using Microsoft.Extensions.Compliance.Classification;

namespace Bookstore.Ordering;

[DataCategory("sign-in-session")]
public sealed class CustomerSession
{
    [NoDataClassification]
    public int Id { get; set; }

    [PersonalData(InformationType = "Customer ID")]
    public int CustomerId { get; set; }

    [NoDataClassification]
    public DateTime SignedInAtUtc { get; set; }

    [PersonalData(InformationType = "Networking")]
    public required string IpAddress { get; set; }

    [PersonalData(InformationType = "Device ID")]
    public required string DeviceId { get; set; }

    [PersonalData(InformationType = "Networking")]
    public required string UserAgent { get; set; }
}
