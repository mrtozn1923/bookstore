namespace Bookstore.Shipping;

public interface IShippingCarrier
{
    Task<ShippingQuote> GetQuoteAsync(ShippingQuoteRequest request, CancellationToken cancellationToken);
}

public sealed record ShippingQuoteRequest(string FromPostalCode, string ToPostalCode, int WeightGrams);

public sealed record ShippingQuote(string CarrierQuoteId, decimal Price, string Currency, int EstimatedDays);

public enum ShippingFailureKind
{
    Transient,
    Unavailable,
    ContractViolation
}

public sealed class ShippingCarrierException(ShippingFailureKind kind, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public ShippingFailureKind Kind { get; } = kind;
}
