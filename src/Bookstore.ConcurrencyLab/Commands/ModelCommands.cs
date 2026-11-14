using System.Diagnostics;
using Bookstore.ConcurrencyLab.Models;

namespace Bookstore.ConcurrencyLab.Commands;

// Post 08: concurrency models
public static class ModelCommands
{
    public static async Task<bool> RunAsync(string command, string[] args)
    {
        switch (command)
        {
            case "models":
                {
                    const int Stock = 1_000, Clients = 8, Orders = 500;
                    (string Name, Func<IOrderDesk> Create)[] desks =
                    [
                        ("shared memory + lock", () => new LockedOrderDesk(Stock)),
                        ("actor (mailbox)", () => new InventoryActor(Stock)),
                        ("CSP pipeline", () => new OrderPipeline(Stock)),
                    ];
                    foreach ((string name, Func<IOrderDesk> create) in desks)
                    {
                        Stopwatch sw = Stopwatch.StartNew();
                        int accepted = await ModelRun.AcceptedAsync(create(), Clients, Orders);
                        Console.WriteLine($"{name,-21} stock={Stock:N0} orders={Clients * Orders:N0} accepted={accepted:N0} ({sw.ElapsedMilliseconds} ms)");
                    }
                    return true;
                }
            case "actor-protocol":
                {
                    for (int run = 1; run <= 3; run++)
                    {
                        InventoryActor actor = new(1_000);
                        int sold = await ModelRun.SoldWithGetThenSetAsync(actor, clients: 8, ordersPerClient: 500);
                        int left = await actor.GetStockAsync();
                        Console.WriteLine($"run {run}: stock=1,000 sold with GetStock+SetStock={sold:N0} stock left={left:N0}");
                    }
                    return true;
                }
        }
        return false;
    }
}
