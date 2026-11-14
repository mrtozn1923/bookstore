using System.Diagnostics;
using Bookstore.ConcurrencyLab.LockFree;
using Bookstore.ConcurrencyLab.Races;

namespace Bookstore.ConcurrencyLab.Commands;

// Post 07: lock-free algorithms
public static class LockFreeCommands
{
    public static bool Run(string command, string[] args)
    {
        switch (command)
        {
            case "cas-counter":
                {
                    foreach (int threads in new[] { 1, 2, 4, 8 })
                    {
                        LockFreeCounter counter = new();
                        long failed = 0;
                        Stopwatch sw = Stopwatch.StartNew();
                        RaceRunner.RunTogether(threads, _ =>
                        {
                            long mine = 0;
                            for (int i = 0; i < 8_000_000 / threads; i++)
                                mine += counter.Add(1);
                            Interlocked.Add(ref failed, mine);
                        });
                        Console.WriteLine($"threads={threads} value={counter.Value:N0} failed CAS={failed,10:N0} ({sw.ElapsedMilliseconds} ms)");
                    }
                    return true;
                }
            case "stack-stress":
                {
                    int perThread = int.Parse(args[1]);
                    (string Name, Func<IConcurrentStack<int>> Create)[] stacks =
                    [
                        ("Treiber (new nodes)", () => new TreiberStack<int>()),
                        ("Treiber (pooled nodes)", () => new PooledTreiberStack()),
                        ("racy read-then-write", () => new RacyStack()),
                    ];
                    foreach ((string name, Func<IConcurrentStack<int>> create) in stacks)
                    {
                        (int missing, int duplicated) = StackStress.Run(create(), threads: 8, perThread);
                        Console.WriteLine($"{name,-23} pushed={8 * perThread:N0} missing={missing,7:N0} popped twice={duplicated,7:N0}");
                    }
                    return true;
                }
            case "linearizability":
                {
                    int rounds = int.Parse(args[1]);
                    foreach ((string name, Func<IConcurrentStack<int>> create) in new (string, Func<IConcurrentStack<int>>)[]
                             { ("Treiber", () => new TreiberStack<int>()), ("racy", () => new RacyStack()) })
                    {
                        int bad = 0;
                        List<StackCall>? example = null;
                        for (int r = 0; r < rounds; r++)
                        {
                            List<StackCall> history = StackHistory.Record(create(), threads: 3, callsPerThread: 4);
                            if (!LinearizabilityChecker.IsLinearizable(history))
                            {
                                bad++;
                                example ??= history;
                            }
                        }
                        Console.WriteLine($"{name,-8} histories={rounds:N0} not linearizable={bad:N0}");
                        if (example is not null)
                            foreach (StackCall call in example)
                                Console.WriteLine($"    {call}");
                    }
                    return true;
                }
        }
        return false;
    }
}
