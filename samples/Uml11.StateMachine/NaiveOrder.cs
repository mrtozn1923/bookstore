namespace Bookstore.Institutional.StateMachines;

// Açılıştaki ilk sürüm: R11 okunarak tek bir method'a yazılmış iptal kuralı
public sealed class NaiveOrder(OrderStatus status, DateOnly deliveryDate)
{
    public OrderStatus Status { get; private set; } = status;
    public DateOnly DeliveryDate { get; } = deliveryDate;

    public void Cancel(DateOnly today)
    {
        if (Status == OrderStatus.Approved && today >= DeliveryDate)
        {
            throw new InvalidOperationException("Delivery date already reached.");
        }
        Status = OrderStatus.Cancelled;
    }
}
