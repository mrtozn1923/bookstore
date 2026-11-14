using StackExchange.Redis;

namespace Bookstore.Worker;

// Reads bookstore:orders:events as one consumer of a consumer group, acknowledges what it processed
public abstract class OrderEventsConsumer(IConnectionMultiplexer redis, IConfiguration config, ILogger logger) : BackgroundService
{
    public const string Stream = "bookstore:orders:events";
    public const string DeadLetters = "bookstore:orders:events:dead";
    private const int MaxDeliveries = 3;
    private static readonly TimeSpan ClaimAfter = TimeSpan.FromSeconds(30);

    protected abstract string Group { get; }

    protected abstract Task HandleAsync(StreamEntry entry, CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IDatabase db = redis.GetDatabase();
        // A stable name: after a restart this instance is the same consumer again
        string consumer = $"{Group}-{config["Worker:ConsumerName"] ?? "worker-1"}";
        await EnsureGroupAsync(db);

        while (!stoppingToken.IsCancellationRequested)
        {
            // XREADGROUP GROUP g c COUNT 10 CLAIM 30000 STREAMS key >: idle entries of other consumers first, then new ones (Redis 8.4+)
            StreamEntry[] entries = await db.StreamReadGroupAsync(Stream, Group, consumer, StreamPosition.NewMessages,
                count: 10, noAck: false, claimMinIdleTime: ClaimAfter);
            if (entries.Length == 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);   // no BLOCK on a shared multiplexer
                continue;
            }

            foreach (StreamEntry entry in entries)
            {
                if (entry.DeliveryCount >= MaxDeliveries)
                {
                    await DeadLetterAsync(db, entry);
                    continue;
                }
                try
                {
                    await HandleAsync(entry, stoppingToken);
                    await db.StreamAcknowledgeAsync(Stream, Group, entry.Id);          // XACK
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning("{Group}: {Id} failed ({Error}), released for retry", Group, entry.Id, ex.Message);
                    await db.StreamNegativeAcknowledgeAsync(Stream, Group, StreamNackMode.Fail, entry.Id); // XNACK ... FAIL (Redis 8.8+)
                }
            }
        }
    }

    private async Task EnsureGroupAsync(IDatabase db)
    {
        try
        {
            // XGROUP CREATE key group 0 MKSTREAM: the group starts from the first event in the stream
            await db.StreamCreateConsumerGroupAsync(Stream, Group, StreamPosition.Beginning, createStream: true);
        }
        catch (RedisServerException ex) when (ex.Message.StartsWith("BUSYGROUP"))
        {
        }
    }

    // A poison message: copy it to the dead-letter stream and acknowledge it, in one MULTI/EXEC
    private async Task DeadLetterAsync(IDatabase db, StreamEntry entry)
    {
        NameValueEntry[] fields = [new("sourceId", entry.Id), new("group", Group), new("deliveries", entry.DeliveryCount), .. entry.Values];
        ITransaction tx = db.CreateTransaction();
        Task<RedisValue> added = tx.StreamAddAsync(DeadLetters, fields);
        Task<long> acked = tx.StreamAcknowledgeAsync(Stream, Group, entry.Id);
        await tx.ExecuteAsync();
        logger.LogError("{Group}: {Id} moved to {DeadLetters} as {DeadId} after {Count} deliveries",
            Group, entry.Id, DeadLetters, await added, entry.DeliveryCount);
        await acked;
    }
}

public sealed class EmailConsumer(IConnectionMultiplexer redis, IConfiguration config, ILogger<EmailConsumer> logger)
    : OrderEventsConsumer(redis, config, logger)
{
    protected override string Group => "emails";

    protected override Task HandleAsync(StreamEntry entry, CancellationToken cancellationToken)
    {
        logger.LogInformation("emails: {Id} confirmation e-mail for order {OrderId} to customer {CustomerId}",
            entry.Id, entry["orderId"], entry["customerId"]);
        return Task.CompletedTask;
    }
}

public sealed class StockConsumer(IConnectionMultiplexer redis, IConfiguration config, ILogger<StockConsumer> logger)
    : OrderEventsConsumer(redis, config, logger)
{
    protected override string Group => "stock";

    protected override Task HandleAsync(StreamEntry entry, CancellationToken cancellationToken)
    {
        int quantity = int.Parse(entry["quantity"]!);                  // a malformed event throws here
        logger.LogInformation("stock: {Id} {Quantity} x {Isbn} picked from the warehouse", entry.Id, quantity, entry["isbn"]);
        return Task.CompletedTask;
    }
}
