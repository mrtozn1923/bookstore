using System.Globalization;

namespace Bookstore.Web.Store;

// The store's standard terms; the policy page and the JSON-LD are both built from this object
public sealed record StorePolicy(
    string Country,
    int ReturnDays,
    decimal ShippingFee,
    decimal FreeShippingFrom,
    int HandlingDaysMax,
    int TransitDaysMin,
    int TransitDaysMax,
    TimeOnly CutoffTime,
    TimeSpan UtcOffset,
    string Currency = "TRY")
{
    // Sample policy: 14 days to return after delivery, free shipping from 500 TL
    public static StorePolicy Bookstore { get; } = new(
        Country: "TR",
        ReturnDays: 14,
        ShippingFee: 49.90m,
        FreeShippingFrom: 500.00m,
        HandlingDaysMax: 1,
        TransitDaysMin: 1,
        TransitDaysMax: 3,
        CutoffTime: new TimeOnly(14, 0),
        UtcOffset: TimeSpan.FromHours(3));

    // ISO 8601 time with offset, e.g. 14:00:00+03:00
    public string CutoffTimeWithOffset =>
        new DateTimeOffset(DateOnly.MinValue.ToDateTime(CutoffTime), UtcOffset)
            .ToString("HH:mm:sszzz", CultureInfo.InvariantCulture);
}
