using System.Diagnostics.Metrics;

namespace Bookstore.Ordering;

// Business metrics of checkout. One Meter per module; the instruments follow OpenTelemetry naming:
// lower case, dot separated, the unit in its own field (UCUM: "s", "{order}").
public sealed class OrderingMetrics
{
    public const string MeterName = "Bookstore.Ordering";

    private readonly Counter<long> ordersPlaced;
    private readonly Histogram<double> checkoutDuration;
    private readonly UpDownCounter<long> activeCheckouts;

    public OrderingMetrics(IMeterFactory meterFactory)
    {
        Meter meter = meterFactory.Create(MeterName);
        ordersPlaced = meter.CreateCounter<long>(
            "bookstore.orders.placed", unit: "{order}", description: "Orders that were placed and paid.");
        checkoutDuration = meter.CreateHistogram(
            "bookstore.checkout.duration", unit: "s", description: "Duration of POST /api/orders, payment included.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = [0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 7.5, 10] });
        activeCheckouts = meter.CreateUpDownCounter<long>(
            "bookstore.checkouts.active", unit: "{checkout}", description: "Checkouts in progress.");
    }

    public void CheckoutStarted() => activeCheckouts.Add(1);

    // outcome: placed | out_of_stock | payment_failed | cart_empty | cart_not_found | cart_changed
    public void CheckoutFinished(string outcome, string orderType, TimeSpan elapsed)
    {
        activeCheckouts.Add(-1);
        KeyValuePair<string, object?>[] tags =
        [
            new("bookstore.checkout.outcome", outcome),
            new("bookstore.order.type", orderType)
        ];
        checkoutDuration.Record(elapsed.TotalSeconds, tags);
        if (outcome == "placed")
        {
            ordersPlaced.Add(1, new KeyValuePair<string, object?>("bookstore.order.type", orderType));
        }
    }
}
