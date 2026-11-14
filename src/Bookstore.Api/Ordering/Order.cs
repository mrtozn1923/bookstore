using System.ComponentModel.DataAnnotations;
using Bookstore.Api.Catalog;

namespace Bookstore.Api.Ordering;

// PartiallyShipped arrived with institutional partial shipments; OrderStatus is open (ADR-0004).
public enum OrderStatus { Pending, Confirmed, PartiallyShipped, Shipped, Delivered, Cancelled }

public sealed record OrderItem(string Isbn, string Title, int Quantity, decimal UnitPrice);

public sealed class Order
{
    public required Guid Id { get; init; }
    public string? CustomerId { get; init; }
    public string? InstitutionId { get; init; }
    public string? UnitId { get; init; }
    public required OrderStatus Status { get; set; }
    public required DateTime CreatedAt { get; init; }
    public required IReadOnlyList<OrderItem> Items { get; init; }
    public decimal Total => Items.Sum(i => i.Quantity * i.UnitPrice);
}

public enum ReturnStatus { Requested, Approved, Rejected, Refunded }

public sealed record OrderReturn(Guid Id, Guid OrderId, ReturnStatus Status, string Reason, DateTime RequestedAt);

public sealed record OrderItemResponse(string Isbn, string Title, int Quantity, Money UnitPrice);

public sealed record OrderResponse(
    Guid Id,
    OrderStatus Status,
    string? CustomerId,
    string? InstitutionId,
    string? UnitId,
    DateTime CreatedAt,
    IReadOnlyList<OrderItemResponse> Items,
    Money Total)
{
    public static OrderResponse From(Order order) =>
        new(order.Id, order.Status, order.CustomerId, order.InstitutionId, order.UnitId, order.CreatedAt,
            [.. order.Items.Select(i => new OrderItemResponse(i.Isbn, i.Title, i.Quantity, new Money(i.UnitPrice, "TRY")))],
            new Money(order.Total, "TRY"));
}

public sealed record CreateReturnRequest(string Reason);

public sealed record CreateOrderItem(
    [property: Required(ErrorMessage = "required"), RegularExpression("^97[89][0-9]{10}$", ErrorMessage = "invalid-format")]
    string Isbn,
    [property: Range(1, 99, ErrorMessage = "out-of-range")] int Quantity);

public sealed record CreateOrderRequest(
    [property: Required(ErrorMessage = "required"), Length(1, 50, ErrorMessage = "out-of-range")]
    IReadOnlyList<CreateOrderItem> Items);
