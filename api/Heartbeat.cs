using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Bookstore.Assistant.Api;

public static class Heartbeat
{
    // Kaynak sessiz kaldıkça her interval'de bir "ping" event'i araya sokar
    public static async IAsyncEnumerable<SseItem<object>> WithHeartbeatAsync(
        this IAsyncEnumerable<SseItem<object>> source,
        TimeSpan interval,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        Channel<SseItem<object>> channel = Channel.CreateUnbounded<SseItem<object>>();
        Task producer = Task.Run(async () =>
        {
            try
            {
                await foreach (SseItem<object> item in source.WithCancellation(ct))
                {
                    await channel.Writer.WriteAsync(item, ct);
                }
                channel.Writer.Complete();
            }
            catch (Exception ex)
            {
                channel.Writer.Complete(ex);
            }
        }, CancellationToken.None);

        Task<bool>? ready = null;
        while (true)
        {
            ready ??= channel.Reader.WaitToReadAsync(ct).AsTask();
            if (await Task.WhenAny(ready, Task.Delay(interval, ct)) != ready)
            {
                yield return new SseItem<object>(new { }, "ping");
                continue;
            }

            if (!await ready)
            {
                break;
            }
            ready = null;

            while (channel.Reader.TryRead(out SseItem<object> item))
            {
                yield return item;
            }
        }

        await producer;
    }
}
