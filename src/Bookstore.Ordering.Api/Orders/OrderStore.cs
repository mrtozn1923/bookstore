using System.Collections.Concurrent;

namespace Bookstore.Ordering.Api.Orders;

// Orders stay in memory: persistence is not this series' topic
public sealed class OrderStore
{
    private readonly ConcurrentDictionary<Guid, Order> orders = new();

    public void Add(Order order) => orders[order.Id] = order;

    public Order? Find(Guid id) => orders.GetValueOrDefault(id);

    public bool Confirm(Guid id)
    {
        if (!orders.TryGetValue(id, out Order? order))
        {
            return false;
        }

        orders[id] = order with { Status = OrderStatus.Confirmed };
        return true;
    }
}
