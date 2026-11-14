Order order = new(id: 42, totalAmount: 56_700m);

order.Submit();
order.Approve();   // onaycı onaylar
order.Cancel();    // kullanıcı vazgeçer
order.Approve();   // satın alma müdürü, eski e-postadaki linke tıklar

Console.WriteLine(order.Status);

public enum OrderStatus { Draft, PendingApprover, PendingPurchasingManager, Approved, Rejected, Cancelled }

public sealed class Order(int id, decimal totalAmount)
{
    public int Id { get; } = id;
    public decimal TotalAmount { get; } = totalAmount;
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;

    public void Submit() => Status = OrderStatus.PendingApprover;

    public void Approve() =>
        Status = Status == OrderStatus.PendingApprover && TotalAmount > 50_000m
            ? OrderStatus.PendingPurchasingManager
            : OrderStatus.Approved;

    public void Cancel()
    {
        if (Status != OrderStatus.Approved)
            Status = OrderStatus.Cancelled;
    }
}
