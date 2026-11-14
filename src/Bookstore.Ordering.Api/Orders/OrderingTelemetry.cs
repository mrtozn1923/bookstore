using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Bookstore.Ordering.Api.Orders;

// Source and meter use the application name, which ServiceDefaults registers with OpenTelemetry
public sealed class OrderingTelemetry
{
    public const string Name = "Bookstore.Ordering.Api";
    public static readonly ActivitySource ActivitySource = new(Name);

    private readonly Counter<long> ordersPlaced;
    private readonly Histogram<double> orderTotal;

    public OrderingTelemetry(IMeterFactory meterFactory)
    {
        Meter meter = meterFactory.Create(Name);
        ordersPlaced = meter.CreateCounter<long>("bookstore.orders.placed", unit: "{order}", description: "Orders placed");
        orderTotal = meter.CreateHistogram<double>("bookstore.orders.total", unit: "TRY", description: "Order total");
    }

    public void OrderPlaced(Order order)
    {
        ordersPlaced.Add(1);
        orderTotal.Record((double)order.Total);
    }
}
