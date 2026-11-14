using System.Collections.Concurrent;
using Lib.Net.Http.WebPush;

namespace Bookstore.Notifications.Api;

// in memory for the series; in Bookstore this is a table keyed by endpoint
public sealed class PushSubscriptionStore
{
    private readonly ConcurrentDictionary<string, (string Customer, PushSubscription Subscription)> subscriptions = new();

    public void Save(string customer, PushSubscription subscription) =>
        subscriptions[subscription.Endpoint] = (customer, subscription);

    public bool Remove(string endpoint) => subscriptions.TryRemove(endpoint, out _);

    public IReadOnlyList<PushSubscription> For(string customer) =>
        [.. subscriptions.Values.Where(s => s.Customer == customer).Select(s => s.Subscription)];
}
