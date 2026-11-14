using System.Diagnostics;
using System.Threading.Channels;
using Bookstore.Observability;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Bookstore.Notifications;

// The message carries its own headers, like a RabbitMQ or Service Bus message would.
public sealed record OrderConfirmationMessage(int OrderId, string CustomerEmail, Dictionary<string, string> Headers);

// An in-process queue between checkout and the e-mail sender. A real broker is the subject of the
// Message Queue series; the trace context travels the same way: written into the message headers.
public sealed class OrderConfirmationQueue
{
    private readonly Channel<OrderConfirmationMessage> channel = Channel.CreateBounded<OrderConfirmationMessage>(1000);
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    public ChannelReader<OrderConfirmationMessage> Reader => channel.Reader;

    public async ValueTask PublishAsync(int orderId, string customerEmail, CancellationToken ct)
    {
        using Activity? activity = BookstoreActivitySources.Notifications.StartActivity("order-confirmations publish", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "in-process");
        activity?.SetTag("messaging.destination.name", "order-confirmations");
        activity?.SetTag("bookstore.order.id", orderId);

        Dictionary<string, string> headers = [];
        // traceparent (and baggage, if any) go into the message, so the consumer can continue the same trace.
        Propagator.Inject(new PropagationContext(activity?.Context ?? default, Baggage.Current), headers,
            static (carrier, key, value) => carrier[key] = value);

        await channel.Writer.WriteAsync(new OrderConfirmationMessage(orderId, customerEmail, headers), ct);
    }
}
