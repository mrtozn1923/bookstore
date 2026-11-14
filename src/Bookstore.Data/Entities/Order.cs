namespace Bookstore.Data.Entities;

public class Order
{
    public int Id { get; set; }
    public Guid RequestId { get; set; }
    public int OrderNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ShippingFee { get; set; } = 4.99m;
    public decimal TotalAmount { get; private set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public List<OrderItem> Items { get; set; } = [];
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int BookId { get; set; }
    public Book Book { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
