Order order = new(id: 42, totalAmount: 56_700m);

order.Submit();
order.Approve();
order.Cancel();

try
{
    order.Approve();
}
catch (InvalidOperationException ex)
{
    Console.WriteLine(ex.Message);
}

Console.WriteLine(order.Status);

public enum OrderStatus { Draft, PendingApprover, PendingPurchasingManager, Approved, Rejected, Cancelled }

public sealed class Order(int id, decimal totalAmount)
{
    public int Id { get; } = id;
    public decimal TotalAmount { get; } = totalAmount;
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;

    public void Submit() => Status = Status == OrderStatus.Draft
        ? OrderStatus.PendingApprover
        : throw Invalid(nameof(Submit));

    // Diyagramdaki her ok bir case; ok yoksa exception
    public void Approve() => Status = Status switch
    {
        OrderStatus.PendingApprover when TotalAmount > 50_000m => OrderStatus.PendingPurchasingManager,
        OrderStatus.PendingApprover => OrderStatus.Approved,
        OrderStatus.PendingPurchasingManager => OrderStatus.Approved,
        _ => throw Invalid(nameof(Approve))
    };

    public void Cancel() => Status = Status switch
    {
        OrderStatus.PendingApprover or OrderStatus.PendingPurchasingManager => OrderStatus.Cancelled,
        _ => throw Invalid(nameof(Cancel))
    };

    private InvalidOperationException Invalid(string action) =>
        new($"Order {Id}: '{action}' is not allowed in status {Status}.");
}
