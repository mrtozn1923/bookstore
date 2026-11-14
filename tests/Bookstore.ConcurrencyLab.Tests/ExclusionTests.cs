using Bookstore.ConcurrencyLab.Exclusion;

namespace Bookstore.ConcurrencyLab.Tests;

public class ExclusionTests
{
    [Fact]
    public void Peterson_with_full_fence_keeps_mutual_exclusion() =>
        Assert.Equal(0, MutualExclusionCheck.LostUpdates(new PetersonLock(fullFence: true), 1_000_000));

    [Fact]
    public void Test_and_set_lock_protects_a_plain_counter()
    {
        TasLock gate = new();
        int counter = 0;
        Thread[] threads = [.. Enumerable.Range(0, 4).Select(_ => new Thread(() =>
        {
            for (int i = 0; i < 250_000; i++)
            {
                gate.Enter();
                counter++;
                gate.Exit();
            }
        }))];

        foreach (Thread t in threads) t.Start();
        foreach (Thread t in threads) t.Join();

        Assert.Equal(1_000_000, counter);
    }

    [Fact]
    public void Bounded_buffer_with_while_delivers_every_parcel_once()
    {
        ShippingQueue queue = new(capacity: 4, recheckWithWhile: true);
        int[] seen = new int[40_000];
        Thread[] consumers = [.. Enumerable.Range(0, 4).Select(_ => new Thread(() =>
        {
            for (int i = 0; i < 10_000; i++)
                Interlocked.Increment(ref seen[queue.Take().OrderId]);
        }))];
        Thread[] producers = [.. Enumerable.Range(0, 2).Select(p => new Thread(() =>
        {
            for (int i = 0; i < 20_000; i++)
                queue.Put(new Parcel(p * 20_000 + i));
        }))];

        foreach (Thread t in consumers.Concat(producers)) t.Start();
        foreach (Thread t in consumers.Concat(producers)) t.Join();

        Assert.All(seen, count => Assert.Equal(1, count));
    }
}
