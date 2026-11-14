using System.Threading.Channels;
using Bookstore.Institutional.Application;
using Bookstore.Institutional.Domain;

namespace Bookstore.Institutional.Infrastructure;

internal sealed class ChannelEventBus : IIntegrationEventPublisher
{
    private readonly Channel<OrderApproved> channel = Channel.CreateUnbounded<OrderApproved>();

    public ChannelReader<OrderApproved> Reader => channel.Reader;

    public ValueTask PublishAsync(OrderApproved @event, CancellationToken ct) =>
        channel.Writer.WriteAsync(@event, ct);
}
