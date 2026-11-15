using Bookstore.Privacy;
using Microsoft.Extensions.Compliance.Classification;

namespace Bookstore.Ordering;

[DataCategory("order-billing")]
public sealed class Order
{
    [NoDataClassification]
    public int Id { get; set; }

    [PersonalData(InformationType = "Customer ID")]
    public int CustomerId { get; set; }

    public Customer? Customer { get; set; }

    [NoDataClassification]
    public DateTime OrderedAtUtc { get; set; }

    [NoDataClassification]
    public decimal Total { get; set; }

    [PersonalData(InformationType = "Contact Info")]
    public required string ShippingAddress { get; set; }

    [PersonalData(InformationType = "Name")]
    public required string BillingName { get; set; }

    // AES-GCM ciphertext written by NationalIdVault; the key is not in the database.
    [SensitiveData(InformationType = "National ID")]
    public string? BillingNationalIdEncrypted { get; set; }

    // HMAC blind index for equality lookups.
    [PersonalData(InformationType = "National ID Index")]
    public string? BillingNationalIdIndex { get; set; }
}
