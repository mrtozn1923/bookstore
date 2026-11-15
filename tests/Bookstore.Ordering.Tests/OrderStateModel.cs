namespace Bookstore.Ordering.Tests;

// The model taken from the UML state machine diagram; from the diagram, not from the code
public static class OrderStateModel
{
    public sealed record Transition(OrderState From, OrderEvent Event, OrderState To, bool Refunds = false);

    // Valid transitions: event [guard] / action
    public static readonly Transition[] Valid =
    [
        new(OrderState.Created, OrderEvent.Pay, OrderState.Paid),
        new(OrderState.Created, OrderEvent.Cancel, OrderState.Cancelled),
        new(OrderState.Paid, OrderEvent.Hold, OrderState.OnHold),
        new(OrderState.Paid, OrderEvent.Ship, OrderState.Shipped),
        new(OrderState.Paid, OrderEvent.Cancel, OrderState.Cancelled, Refunds: true),
        new(OrderState.OnHold, OrderEvent.Release, OrderState.Paid),
        new(OrderState.OnHold, OrderEvent.Cancel, OrderState.Cancelled, Refunds: true),
        new(OrderState.Shipped, OrderEvent.Deliver, OrderState.Delivered),
        new(OrderState.Delivered, OrderEvent.Return, OrderState.Returned, Refunds: true), // [within 14 days of delivery]
    ];

    // The shortest event sequence from Created to each state
    public static readonly Dictionary<OrderState, OrderEvent[]> ShortestPath = new()
    {
        [OrderState.Created] = [],
        [OrderState.Paid] = [OrderEvent.Pay],
        [OrderState.OnHold] = [OrderEvent.Pay, OrderEvent.Hold],
        [OrderState.Shipped] = [OrderEvent.Pay, OrderEvent.Ship],
        [OrderState.Delivered] = [OrderEvent.Pay, OrderEvent.Ship, OrderEvent.Deliver],
        [OrderState.Cancelled] = [OrderEvent.Cancel],
        [OrderState.Returned] = [OrderEvent.Pay, OrderEvent.Ship, OrderEvent.Deliver, OrderEvent.Return],
    };
}
