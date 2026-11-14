using System.Net;
using System.Text.Json;
using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;
using Microsoft.Extensions.Options;

namespace Bookstore.Notifications.Api;

public sealed record NotifyResult(int Sent, int Removed, int Failed);

public sealed class OrderNotifier(
    PushServiceClient pushClient,
    PushSubscriptionStore store,
    IOptions<PushOptions> options,
    ILogger<OrderNotifier> logger)
{
    public async Task<NotifyResult> OrderShippedAsync(string customer, int orderId, CancellationToken ct)
    {
        // the payload format the Angular service worker understands
        string payload = JsonSerializer.Serialize(new
        {
            notification = new
            {
                title = "Your order has shipped",
                body = $"Order {orderId} is on its way.",
                icon = "icons/icon-192.png",
                tag = $"order-{orderId}",
                data = new
                {
                    onActionClick = new
                    {
                        @default = new { operation = "navigateLastFocusedOrOpen", url = "/orders" },
                    },
                },
            },
        });
        PushMessage message = new(payload)
        {
            Topic = $"order-{orderId}", // a newer message with the same topic replaces an undelivered one
            Urgency = PushMessageUrgency.Normal,
            TimeToLive = 24 * 60 * 60, // seconds; after a day the news is old
        };
        PushOptions push = options.Value;
        VapidAuthentication vapid = new(push.PublicKey, push.PrivateKey) { Subject = push.Subject };

        int sent = 0, removed = 0, failed = 0;
        foreach (PushSubscription subscription in store.For(customer))
        {
            try
            {
                await pushClient.RequestPushMessageDeliveryAsync(subscription, message, vapid, ct);
                sent++;
            }
            catch (PushServiceClientException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
            {
                // the browser unsubscribed or the subscription expired: it will never work again
                store.Remove(subscription.Endpoint);
                removed++;
                logger.LogInformation("Removed subscription {Endpoint}: {Status}", subscription.Endpoint, (int)ex.StatusCode);
            }
            catch (PushServiceClientException ex)
            {
                // 400/413: our request is wrong; 429/5xx: try again later (a background job, not this request)
                failed++;
                logger.LogWarning("Push to {Endpoint} failed: {Status} {Reason}", subscription.Endpoint, (int)ex.StatusCode, ex.Message);
            }
        }
        logger.LogInformation("Order {OrderId} shipped: {Sent} sent, {Removed} removed, {Failed} failed", orderId, sent, removed, failed);
        return new NotifyResult(sent, removed, failed);
    }
}
