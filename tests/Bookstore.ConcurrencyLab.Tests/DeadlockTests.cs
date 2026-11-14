using Bookstore.ConcurrencyLab.Deadlocks;

namespace Bookstore.ConcurrencyLab.Tests;

public class DeadlockTests
{
    [Fact]
    public void Ordered_locking_finishes_opposite_transfers_and_keeps_total_stock()
    {
        Warehouse istanbul = new(1, "Istanbul"), ankara = new(2, "Ankara");
        istanbul.Stock["9780441013593"] = 1_000_000;
        ankara.Stock["9780441013593"] = 1_000_000;

        Thread east = new(() => { for (int i = 0; i < 200_000; i++) StockTransfer.MoveOrdered(istanbul, ankara, "9780441013593", 1); });
        Thread west = new(() => { for (int i = 0; i < 200_000; i++) StockTransfer.MoveOrdered(ankara, istanbul, "9780441013593", 1); });
        east.Start(); west.Start();

        Assert.True(east.Join(TimeSpan.FromSeconds(30)) && west.Join(TimeSpan.FromSeconds(30)));
        Assert.Equal(2_000_000, istanbul.Stock["9780441013593"] + ankara.Stock["9780441013593"]);
    }

    [Fact]
    public void Try_move_gives_up_when_the_other_lock_is_held()
    {
        Warehouse istanbul = new(1, "Istanbul"), ankara = new(2, "Ankara");
        using ManualResetEventSlim held = new(), release = new();
        Thread holder = new(() => { lock (ankara.Gate) { held.Set(); release.Wait(); } });
        holder.Start();
        held.Wait();

        bool moved = StockTransfer.TryMove(istanbul, ankara, "9780441013593", 1, TimeSpan.FromMilliseconds(100));

        release.Set();
        holder.Join();
        Assert.False(moved);
        Assert.False(istanbul.Stock.ContainsKey("9780441013593"));
    }

    [Fact]
    public void Wait_for_graph_finds_a_cycle_only_when_there_is_one()
    {
        WaitForGraph chain = new();
        chain.AddWait("T1", "T2");
        chain.AddWait("T2", "T3");
        Assert.Empty(chain.FindCycle());

        WaitForGraph cycle = new();
        cycle.AddWait("T1", "T2");
        cycle.AddWait("T2", "T3");
        cycle.AddWait("T3", "T1");
        Assert.Equal(["T1", "T2", "T3", "T1"], cycle.FindCycle());
    }

    [Fact]
    public void Philosophers_with_a_global_fork_order_always_finish() =>
        Assert.True(Philosophers.Dine(seats: 5, meals: 100_000, orderedForks: true, TimeSpan.FromSeconds(30)));
}
