using System.Text.Json;
using Bookstore.Contracts;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Bookstore.Ordering.Api.Orders;

// Inventory's reply: the order is confirmed once stock is reserved
public sealed class StockReservedConsumer(IConnection connection, OrderStore store, ILogger<StockReservedConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using IChannel channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(Queues.StockReserved, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, stoppingToken);

        AsyncEventingBasicConsumer consumer = new(channel);
        consumer.ReceivedAsync += async (_, delivery) =>
        {
            StockReserved? message = JsonSerializer.Deserialize<StockReserved>(delivery.Body.Span, JsonSerializerOptions.Web);
            if (message is not null && store.Confirm(message.OrderId))
            {
                logger.LogInformation("Order {OrderId} confirmed", message.OrderId);
            }

            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false);
        };

        await channel.BasicConsumeAsync(Queues.StockReserved, autoAck: false, consumer, stoppingToken);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
