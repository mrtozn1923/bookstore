using System.Text.Json;
using Bookstore.Contracts;
using RabbitMQ.Client;

namespace Bookstore.Ordering.Api.Orders;

// One channel for the whole service; RabbitMQ channels must not be used by two threads at once
public sealed class OrderEventPublisher(IConnection connection) : IAsyncDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private IChannel? channel;

    public async Task PublishAsync(OrderPlaced message, CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            channel ??= await CreateChannelAsync(ct);
            BasicProperties properties = new()
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = message.OrderId.ToString()
            };

            // Publisher confirms are on: this await returns only after the broker has stored the message
            await channel.BasicPublishAsync(exchange: "", routingKey: Queues.OrderPlaced, mandatory: true,
                basicProperties: properties, body: JsonSerializer.SerializeToUtf8Bytes(message, JsonSerializerOptions.Web), cancellationToken: ct);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<IChannel> CreateChannelAsync(CancellationToken ct)
    {
        IChannel created = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true), ct);
        await created.QueueDeclareAsync(Queues.OrderPlaced, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        return created;
    }

    public async ValueTask DisposeAsync()
    {
        if (channel is not null)
        {
            await channel.DisposeAsync();
        }

        gate.Dispose();
    }
}
