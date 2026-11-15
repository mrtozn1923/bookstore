using Bookstore.Privacy;
using Microsoft.Extensions.Compliance.Classification;

namespace Bookstore.Payments;

// No card number, no CVV: the payment provider keeps the card and returns a token.
[DataCategory("payment")]
public sealed class Payment
{
    [NoDataClassification]
    public int Id { get; set; }

    [NoDataClassification]
    public int OrderId { get; set; }

    [SensitiveData(InformationType = "Payment Token")]
    public required string ProviderToken { get; set; }

    [PersonalData(InformationType = "Credit Card")]
    public required string CardLast4 { get; set; }

    [NoDataClassification]
    public decimal AmountTry { get; set; }
}
