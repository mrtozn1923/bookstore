using System.Collections.Concurrent;

namespace Bookstore.Ordering.Api.Orders;

// Orders stay in memory: persistence is not this series' topic
public sealed class OrderStore
{
    private readonly ConcurrentDictionary<Guid, Order> orders = new();

    public void Add(Order order) => orders[order.Id] = order;

    public Order? Find(Guid id) => orders.GetValueOrDefault(id);
}
