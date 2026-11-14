using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Ordering;

public enum OrderStatus
{
    Placed,
    Shipped,
    Cancelled,
}

public record OrderLine(string Isbn, string Title, int Quantity, decimal UnitPrice);

public record OrderDto(int Id, string Customer, IReadOnlyList<OrderLine> Lines, decimal Total, OrderStatus Status);

public record OrderLineRequest(string Isbn, [Range(1, 500)] int Quantity);

public record CreateOrderRequest([MinLength(1)] List<OrderLineRequest> Items);

public class OrderStore
{
    private readonly ConcurrentDictionary<int, OrderDto> _orders = new();
    private int _nextId = 1000;

    public OrderDto Add(string customer, IReadOnlyList<OrderLine> lines)
    {
        int id = Interlocked.Increment(ref _nextId);
        OrderDto order = new(id, customer, lines, lines.Sum(l => l.Quantity * l.UnitPrice), OrderStatus.Placed);
        _orders[id] = order;
        return order;
    }

    public OrderDto? Find(int id) => _orders.GetValueOrDefault(id);

    public IEnumerable<OrderDto> ForCustomer(string customer) =>
        _orders.Values.Where(o => o.Customer == customer).OrderBy(o => o.Id);

    public void Update(OrderDto order) => _orders[order.Id] = order;
}
