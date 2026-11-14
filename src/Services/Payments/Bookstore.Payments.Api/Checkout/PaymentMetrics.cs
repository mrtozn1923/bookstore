using System.Diagnostics.Metrics;
using Bookstore.Contracts.Payments;

namespace Bookstore.Payments.Checkout;

// How payments end and where we learned it. The failure rate is failed / completed; a growing share of
// "reconciliation" means webhooks are not arriving.
public sealed class PaymentMetrics
{
    private readonly Counter<long> completed;
    private readonly Counter<long> unknownResults;

    public PaymentMetrics(IMeterFactory meterFactory)
    {
        Meter meter = meterFactory.Create("Bookstore.Payments");
        completed = meter.CreateCounter<long>("bookstore.payments.completed", unit: "{payment}",
            description: "Payments with a final result, by outcome and by where the result came from.");
        unknownResults = meter.CreateCounter<long>("bookstore.payments.unknown_results", unit: "{charge}",
            description: "Charge calls that ended without an answer (timeout, 5xx); reconciliation has to find out.");
    }

    // source: answer | webhook | reconciliation. Nothing is recorded when the payment was already final.
    public void Completed(object? paymentEvent, string source)
    {
        string? outcome = paymentEvent switch
        {
            PaymentSucceeded => "succeeded",
            PaymentFailed => "failed",
            _ => null
        };
        if (outcome is not null)
        {
            completed.Add(1,
                new KeyValuePair<string, object?>("bookstore.payment.outcome", outcome),
                new KeyValuePair<string, object?>("bookstore.payment.result_source", source));
        }
    }

    public void UnknownResult() => unknownResults.Add(1);
}
