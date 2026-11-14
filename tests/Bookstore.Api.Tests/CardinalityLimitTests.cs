using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace Bookstore.Api.Tests;

// A learning test: what the OpenTelemetry SDK does when a tag has too many distinct values.
public sealed class CardinalityLimitTests
{
    [Fact]
    public void An_order_id_tag_hits_the_cardinality_limit()
    {
        List<Metric> exported = [];
        using Meter meter = new("Bookstore.CardinalityDemo");
        using MeterProvider provider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(meter.Name)
            .AddInMemoryExporter(exported)
            .Build();

        Counter<long> placed = meter.CreateCounter<long>("bookstore.orders.placed");
        for (int orderId = 1; orderId <= 2500; orderId++)
        {
            // Wrong on purpose: every order becomes its own time series.
            placed.Add(1, new KeyValuePair<string, object?>("bookstore.order.id", orderId));
        }
        provider.ForceFlush();

        List<MetricPoint> points = [];
        foreach (MetricPoint point in exported.Single().GetMetricPoints())
        {
            points.Add(point);
        }
        MetricPoint overflow = points.Single(p => HasTag(p, "otel.metric.overflow"));

        // 2000 order ids got their own series; the remaining 500 were merged into the overflow series.
        Assert.Equal(2001, points.Count);
        Assert.Equal(500, overflow.GetSumLong());
    }

    private static bool HasTag(MetricPoint point, string key)
    {
        foreach (KeyValuePair<string, object?> tag in point.Tags)
        {
            if (tag.Key == key)
            {
                return true;
            }
        }
        return false;
    }
}
