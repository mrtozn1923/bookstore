using System.Diagnostics.Metrics;
using Bookstore.Contracts.Payments;
using Bookstore.Payments.Checkout;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

namespace Bookstore.Payments.Tests;

public sealed class PaymentMetricsTests
{
    private readonly IMeterFactory meterFactory = new ServiceCollection().AddMetrics().BuildServiceProvider().GetRequiredService<IMeterFactory>();

    [Fact]
    public void A_final_result_is_counted_with_its_outcome_and_source()
    {
        using MetricCollector<long> completed = new(meterFactory, "Bookstore.Payments", "bookstore.payments.completed");
        PaymentMetrics metrics = new(meterFactory);

        metrics.Completed(new PaymentFailed(Guid.CreateVersion7(), "card_declined"), "webhook");
        // Already final: Payment.Complete returned null, nothing to count
        metrics.Completed(null, "webhook");

        CollectedMeasurement<long> measurement = Assert.Single(completed.GetMeasurementSnapshot());
        Assert.True(measurement.MatchesTags(
            new KeyValuePair<string, object?>("bookstore.payment.outcome", "failed"),
            new KeyValuePair<string, object?>("bookstore.payment.result_source", "webhook")));
    }
}
