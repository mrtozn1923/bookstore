using System.Text.Json.Serialization;

namespace Bookstore.Api.Data;

public sealed class Book
{
    public int Id { get; set; }
    public required string Isbn { get; set; }
    public required string Title { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

public sealed class Coupon
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public int PercentOff { get; set; }
    public DateTimeOffset ValidUntil { get; set; }
}

public sealed class Customer
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string Name { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter<OrderStatus>))]
public enum OrderStatus
{
    Placed,
    Cancelled
}

public sealed class Order
{
    public int Id { get; set; }
    public required string Reference { get; set; }
    public int CustomerId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public OrderStatus Status { get; set; }
    public decimal Total { get; set; }
    public string? PaymentId { get; set; }
    public string? TrackingNumber { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}

public sealed class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int BookId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
