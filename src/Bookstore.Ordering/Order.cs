using System.ComponentModel;

namespace Bookstore.Ordering;

public enum OrderStatus { Draft, Placed, Cancelled }

public sealed record OrderLine(string Isbn, int Quantity, decimal UnitPrice);

public sealed record OrderPlacedEventArgs(Guid OrderId, string OrderNumber, decimal Total);

public sealed class Order : INotifyPropertyChanged
{
    private OrderStatus status = OrderStatus.Draft;

    public Order(Guid id, string orderNumber, string customerEmail, IEnumerable<OrderLine> lines)
    {
        Id = id;
        OrderNumber = orderNumber;
        CustomerEmail = customerEmail.Trim().ToLowerInvariant();
        Lines = [.. lines];
    }

    public Guid Id { get; }
    public string OrderNumber { get; }
    public string CustomerEmail { get; }
    public IReadOnlyList<OrderLine> Lines { get; }
    public decimal Total => Lines.Sum(l => l.UnitPrice * l.Quantity);

    public OrderStatus Status
    {
        get => status;
        private set
        {
            status = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<OrderPlacedEventArgs>? Placed;

    public static Order FromCart(Guid id, string orderNumber, string customerEmail, Cart cart) =>
        new(id, orderNumber, customerEmail, cart.Lines.Select(l => new OrderLine(l.Isbn, l.Quantity, l.UnitPrice)));

    public void MarkPlaced()
    {
        if (Status != OrderStatus.Draft)
        {
            throw new InvalidOperationException($"Order {OrderNumber} is already {Status}.");
        }

        Status = OrderStatus.Placed;
        Placed?.Invoke(this, new OrderPlacedEventArgs(Id, OrderNumber, Total));
    }
}

public static class OrderNumber
{
    public static string Format(int year, int sequence) => $"BS-{year}-{sequence:D6}";
}
