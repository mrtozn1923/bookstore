using System.Text.Json;
using Bookstore.Contracts;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Bookstore.Inventory;

public sealed class OrderPlacedConsumer(
    IConnection connection,
    StockLedger ledger,
    TimeProvider clock,
    ILogger<OrderPlacedConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using IChannel channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            stoppingToken);

        await channel.QueueDeclareAsync(Queues.OrderPlaced, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(Queues.StockReserved, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, stoppingToken);

        AsyncEventingBasicConsumer consumer = new(channel);
        consumer.ReceivedAsync += async (_, delivery) =>
        {
            OrderPlaced? message = JsonSerializer.Deserialize<OrderPlaced>(delivery.Body.Span, JsonSerializerOptions.Web);
            if (message is null)
            {
                // A message we cannot read goes to the dead-letter path, not back to the queue
                await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            ledger.Reserve(message.Items);
            logger.LogInformation("Reserved stock for order {OrderId} ({ItemCount} items)", message.OrderId, message.Items.Count);

            StockReserved reply = new(message.OrderId, clock.GetUtcNow());
            BasicProperties properties = new() { ContentType = "application/json", DeliveryMode = DeliveryModes.Persistent };
            await channel.BasicPublishAsync(exchange: "", routingKey: Queues.StockReserved, mandatory: true,
                basicProperties: properties, body: JsonSerializer.SerializeToUtf8Bytes(reply, JsonSerializerOptions.Web));

            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false);
        };

        await channel.BasicConsumeAsync(Queues.OrderPlaced, autoAck: false, consumer, stoppingToken);

        // Keep the channel open until the host stops
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
