using System.Net;

namespace Bookstore.Shipping;

public sealed record ParcelStatus(
    string TrackingNumber, string State, DateTimeOffset? DeliveredAtUtc, IReadOnlyList<ParcelEvent> Events);

public sealed record ParcelEvent(DateTimeOffset AtUtc, string Location, string Description);

public sealed record ShipmentRequest(string OrderNumber, string RecipientName, string City, string PostalCode, decimal WeightKg);

public sealed record ShipmentCreated(string TrackingNumber);

// Everything that goes wrong on the carrier side surfaces as this one exception type
public sealed class CarrierException(
    string message, HttpStatusCode? statusCode = null, TimeSpan? retryAfter = null, Exception? innerException = null)
    : Exception(message, innerException)
{
    public HttpStatusCode? StatusCode { get; } = statusCode;

    public TimeSpan? RetryAfter { get; } = retryAfter;
}
