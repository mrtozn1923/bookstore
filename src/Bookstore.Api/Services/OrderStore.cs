using Bookstore.Api.Models;

namespace Bookstore.Api.Services;

// in-memory orders for the authorization examples
public class OrderStore
{
    private readonly List<Order> _orders = new()
    {
        new Order { Id = 1, CustomerId = 4 },
        new Order { Id = 2, CustomerId = 5 }
    };

    public Order? Find(int id) => _orders.FirstOrDefault(o => o.Id == id);
}
