using System.Diagnostics;
using Bookstore.ConcurrencyLab.Hardware;
using Bookstore.ConcurrencyLab.MemoryModel;
using Bookstore.ConcurrencyLab.Races;

namespace Bookstore.ConcurrencyLab.Commands;

// Post 06: the .NET memory model
public static class MemoryModelCommands
{
    public static bool Run(string command, string[] args)
    {
        switch (command)
        {
            case "stop-flag":
                {
                    (string Name, Action<PollingWorker> Loop)[] loops =
                    [
                        ("plain field", w => w.RunPlain()),
                        ("Volatile.Read", w => w.RunVolatileRead()),
                        ("volatile field", w => w.RunVolatileField()),
                    ];
                    foreach ((string name, Action<PollingWorker> loop) in loops)
                    {
                        Stopwatch sw = Stopwatch.StartNew();
                        (bool ended, long spins) = StopFlagCheck.Run(loop, TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(3));
                        string result = ended ? $"stopped after {sw.ElapsedMilliseconds} ms, {spins:N0} spins" : "still running after 3 s";
                        Console.WriteLine($"{name,-15} {result}");
                    }
                    return true;
                }
            case "double-check":
                {
                    int rounds = int.Parse(args[1]);
                    LitmusTest[] tests =
                    [
                        new DoubleCheckTest("bool flag", () => new FlagCheckedCache()),
                        new DoubleCheckTest("reference check", () => new ReferenceCheckedCache()),
                        new DoubleCheckTest("Lazy<T>", () => new LazyCache()),
                    ];
                    foreach (LitmusTest test in tests)
                    {
                        long[,] o = new LitmusRunner(test).Run(rounds);
                        long bad = 2 * rounds - (2 * o[1, 1] + o[1, 0] + o[0, 1]);
                        Console.WriteLine($"{test.Name,-16} calls={2L * rounds:N0} null or empty price list={bad:N0}");
                    }
                    return true;
                }
            case "publish":
                {
                    int rounds = int.Parse(args[1]);
                    long[,] o = new LitmusRunner(new PublicationTest()).Run(rounds);
                    Console.WriteLine($"not yet={o[0, 0]:N0} complete object={o[0, 1]:N0} default fee={o[0, 2]:N0}");
                    return true;
                }
            case "volatile-count":
                {
                    VolatileSoldCounter counter = new();
                    RaceRunner.RunTogether(8, _ =>
                    {
                        for (int i = 0; i < 1_000_000; i++)
                            counter.RecordSale();
                    });
                    Console.WriteLine($"expected=8,000,000 sold={counter.Sold:N0}");
                    return true;
                }
        }
        return false;
    }
}
