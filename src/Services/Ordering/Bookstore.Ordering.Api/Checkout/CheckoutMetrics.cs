using System.Diagnostics.Metrics;

namespace Bookstore.Ordering.Checkout;

// Business metrics: how many orders come in, how long checkout takes and how it ends.
// Tags stay low-cardinality: an outcome and a reason category, never an order id or an ISBN.
public sealed class CheckoutMetrics
{
    public const string MeterName = "Bookstore.Ordering";

    private readonly Counter<long> ordersPlaced;
    private readonly Histogram<double> checkoutDuration;

    public CheckoutMetrics(IMeterFactory meterFactory)
    {
        Meter meter = meterFactory.Create(MeterName);
        ordersPlaced = meter.CreateCounter<long>("bookstore.orders.placed", unit: "{order}",
            description: "Orders accepted by Ordering (202), before stock and payment are known.");
        checkoutDuration = meter.CreateHistogram<double>("bookstore.checkout.duration", unit: "s",
            description: "From the order being placed to the saga's end: confirmed or cancelled.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = [0.1, 0.25, 0.5, 1, 2.5, 5, 10, 30, 60, 300, 900] });
    }

    public void OrderPlaced(string currency) =>
        ordersPlaced.Add(1, new KeyValuePair<string, object?>("bookstore.currency", currency));

    // outcome: confirmed | cancelled; reason: none | out-of-stock | payment-failed | payment-timeout
    public void CheckoutEnded(string outcome, string reason, TimeSpan elapsed) =>
        checkoutDuration.Record(elapsed.TotalSeconds,
            new KeyValuePair<string, object?>("bookstore.checkout.outcome", outcome),
            new KeyValuePair<string, object?>("bookstore.checkout.reason", reason));
}
