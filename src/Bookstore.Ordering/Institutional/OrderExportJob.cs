namespace Bookstore.Ordering.Institutional;

// Template method: the base class runs the loop, subclasses load and publish
public abstract class OrderExportJob
{
    public async Task<int> RunAsync(CancellationToken ct)
    {
        IReadOnlyList<Order> pending = await LoadPendingAsync(ct);
        foreach (Order order in pending)
        {
            await PublishAsync(order, ct);
        }

        return pending.Count;
    }

    protected abstract Task<IReadOnlyList<Order>> LoadPendingAsync(CancellationToken ct);

    protected abstract Task PublishAsync(Order order, CancellationToken ct);
}
