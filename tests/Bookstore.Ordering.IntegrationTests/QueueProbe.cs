using System.Text;
using System.Threading.Channels;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Bookstore.Ordering.IntegrationTests;

public sealed record ReceivedMessage(string? Type, string Body, IReadOnlyDictionary<string, string> Headers);

// A queue the test owns. Messages are pushed to the test as they arrive (a signal, not polling).
public sealed class QueueProbe : IAsyncDisposable
{
    private readonly Channel<ReceivedMessage> received = Channel.CreateUnbounded<ReceivedMessage>();
    private IConnection connection = null!;
    private IChannel channel = null!;

    public static async Task<QueueProbe> StartAsync(string uri, string queue, string? bindToExchange = null)
    {
        QueueProbe probe = new();
        probe.connection = await new ConnectionFactory { Uri = new Uri(uri) }.CreateConnectionAsync();
        probe.channel = await probe.connection.CreateChannelAsync();
        await probe.channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false);
        if (bindToExchange is not null)
        {
            await probe.channel.QueueBindAsync(queue, bindToExchange, routingKey: "");
        }

        AsyncEventingBasicConsumer consumer = new(probe.channel);
        consumer.ReceivedAsync += async (_, delivery) =>
        {
            Dictionary<string, string> headers = (delivery.BasicProperties.Headers ?? new Dictionary<string, object?>())
                .ToDictionary(h => h.Key, h => h.Value is byte[] bytes ? Encoding.UTF8.GetString(bytes) : h.Value?.ToString() ?? "");
            await probe.received.Writer.WriteAsync(new(delivery.BasicProperties.Type, Encoding.UTF8.GetString(delivery.Body.Span), headers));
        };
        await probe.channel.BasicConsumeAsync(queue, autoAck: true, consumer);
        return probe;
    }

    // Waits for the next message of the given type; other messages on the queue are skipped
    public async Task<ReceivedMessage> NextAsync(string type, TimeSpan timeout)
    {
        using CancellationTokenSource cts = new(timeout);
        while (true)
        {
            ReceivedMessage message = await received.Reader.ReadAsync(cts.Token);
            if (message.Type == type)
            {
                return message;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await channel.DisposeAsync();
        await connection.DisposeAsync();
    }
}
