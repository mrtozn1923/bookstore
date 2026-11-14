namespace Bookstore.ConcurrencyLab.Models;

public static class ModelRun
{
    // `clients` concurrent buyers each place `ordersPerClient` single-copy orders; returns how many were accepted
    public static async Task<int> AcceptedAsync(IOrderDesk desk, int clients, int ordersPerClient)
    {
        int accepted = 0;
        await Task.WhenAll(Enumerable.Range(0, clients).Select(_ => Task.Run(async () =>
        {
            for (int i = 0; i < ordersPerClient; i++)
                if (await desk.ReserveAsync(1))
                    Interlocked.Increment(ref accepted);
        })));
        return accepted;
    }

    // The actor is safe, the protocol is not: read the stock in one message, write the new value in another
    public static async Task<int> SoldWithGetThenSetAsync(InventoryActor actor, int clients, int ordersPerClient)
    {
        int sold = 0;
        await Task.WhenAll(Enumerable.Range(0, clients).Select(_ => Task.Run(async () =>
        {
            for (int i = 0; i < ordersPerClient; i++)
            {
                int stock = await actor.GetStockAsync();
                if (stock > 0)
                {
                    actor.SetStock(stock - 1);
                    Interlocked.Increment(ref sold);
                }
            }
        })));
        return sold;
    }
}
