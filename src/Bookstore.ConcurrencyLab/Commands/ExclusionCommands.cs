using System.Diagnostics;
using Bookstore.ConcurrencyLab.Exclusion;

namespace Bookstore.ConcurrencyLab.Commands;

// Post 03: mutual exclusion
public static class ExclusionCommands
{
    public static bool Run(string command, string[] args)
    {
        switch (command)
        {
            case "mutex-attempts":
                {
                    int rounds = int.Parse(args[1]);
                    (string Name, ITwoThreadLock Gate)[] gates =
                    [
                        ("flag only", new FlagOnlyLock()),
                        ("Peterson, volatile only", new PetersonLock(fullFence: false)),
                        ("Peterson + full fence", new PetersonLock(fullFence: true)),
                    ];
                    foreach ((string name, ITwoThreadLock gate) in gates)
                    {
                        Stopwatch sw = Stopwatch.StartNew();
                        int lost = MutualExclusionCheck.LostUpdates(gate, rounds);
                        Console.WriteLine($"{name,-24} lost updates: {lost,9:N0} of {2 * rounds:N0}  ({sw.ElapsedMilliseconds} ms)");
                    }
                    return true;
                }
            case "shipping-queue":
                {
                    foreach (bool useWhile in new[] { false, true })
                    {
                        ShippingQueue queue = new(capacity: 4, recheckWithWhile: useWhile);
                        int failures = 0, delivered = 0;
                        Thread[] consumers = [.. Enumerable.Range(0, 4).Select(_ => new Thread(() =>
                        {
                            int taken = 0;
                            while (taken < 50_000)
                            {
                                try { queue.Take(); taken++; Interlocked.Increment(ref delivered); }
                                catch (InvalidOperationException) { Interlocked.Increment(ref failures); }
                            }
                        }))];
                        Thread[] producers = [.. Enumerable.Range(0, 2).Select(p => new Thread(() =>
                        {
                            for (int i = 0; i < 100_000; i++)
                                queue.Put(new Parcel(p * 100_000 + i));
                        }))];
                        foreach (Thread t in consumers.Concat(producers)) t.Start();
                        foreach (Thread t in consumers.Concat(producers)) t.Join();
                        Console.WriteLine($"{(useWhile ? "while" : "if"),-5} delivered={delivered:N0} Take() on empty queue={failures:N0}");
                    }
                    return true;
                }
        }
        return false;
    }
}
