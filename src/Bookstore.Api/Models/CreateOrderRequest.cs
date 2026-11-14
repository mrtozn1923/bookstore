namespace Bookstore.Api.Models;

public class CreateOrderRequest
{
    public string CustomerEmail { get; set; } = string.Empty;
    public List<OrderItemRequest> Items { get; set; } = new();
}

public class OrderItemRequest
{
    public int BookId { get; set; }
    public int Quantity { get; set; }
}
