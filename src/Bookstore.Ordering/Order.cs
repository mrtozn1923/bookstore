namespace Bookstore.Ordering;

public enum OrderState
{
    Created,
    Paid,
    OnHold,
    Shipped,
    Delivered,
    Cancelled,
    Returned,
}

public enum OrderEvent
{
    Pay,
    Hold,
    Release,
    Ship,
    Deliver,
    Cancel,
    Return,
}

public sealed class Order(TimeProvider clock)
{
    public static readonly TimeSpan ReturnWindow = TimeSpan.FromDays(14);

    private readonly List<string> _actions = [];
    private bool _stockReserved;
    private DateTimeOffset? _deliveredAt;

    public OrderState State { get; private set; } = OrderState.Created;

    public IReadOnlyList<string> Actions => _actions;

    public void Fire(OrderEvent e)
    {
        switch (e, State)
        {
            case (OrderEvent.Pay, OrderState.Created):
                _stockReserved = true;
                State = OrderState.Paid;
                break;
            case (OrderEvent.Hold, OrderState.Paid):
                _stockReserved = false;
                State = OrderState.OnHold;
                break;
            case (OrderEvent.Release, OrderState.OnHold):
                _stockReserved = true;
                State = OrderState.Paid;
                break;
            case (OrderEvent.Ship, OrderState.Paid):
                if (!_stockReserved)
                {
                    throw new InvalidOperationException("Cannot ship an order without a stock reservation.");
                }

                _stockReserved = false;
                State = OrderState.Shipped;
                break;
            case (OrderEvent.Deliver, OrderState.Shipped):
                _deliveredAt = clock.GetUtcNow();
                State = OrderState.Delivered;
                break;
            case (OrderEvent.Cancel, OrderState.Created or OrderState.Paid or OrderState.OnHold):
                if (State != OrderState.Created)
                {
                    _actions.Add("Refund");
                }

                _stockReserved = false;
                State = OrderState.Cancelled;
                break;
            case (OrderEvent.Return, OrderState.Delivered) when clock.GetUtcNow() - _deliveredAt <= ReturnWindow:
                _actions.Add("Refund");
                State = OrderState.Returned;
                break;
            default:
                throw new InvalidOperationException($"{e} is not allowed when the order is {State}.");
        }
    }
}
