using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace Bookstore.Api;

// Listens for price changes and removes the book from this instance's L1 cache
public sealed class PriceInvalidationListener(IConnectionMultiplexer redis, IMemoryCache local,
    ILogger<PriceInvalidationListener> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Messages sent while the connection was down are lost: start over with an empty L1
        redis.ConnectionRestored += (_, e) =>
        {
            if (e.ConnectionType == ConnectionType.Subscription && local is MemoryCache memory)
            {
                memory.Clear();
                logger.LogWarning("Subscription connection restored, L1 price cache cleared");
            }
        };

        ChannelMessageQueue queue = await redis.GetSubscriber().SubscribeAsync(RedisKeys.PriceChanged);
        try
        {
            await foreach (ChannelMessage message in queue.WithCancellation(stoppingToken))
            {
                local.Remove(BookPriceEndpoints.CacheKey(message.Message!));
                logger.LogInformation("Price of {Isbn} changed, L1 entry removed", (string?)message.Message);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            await queue.UnsubscribeAsync();
        }
    }
}
