using System.Diagnostics;
using Bookstore.ConcurrencyLab.Hardware;

namespace Bookstore.ConcurrencyLab.Commands;

// Post 05: hardware memory models
public static class HardwareCommands
{
    public static bool Run(string command, string[] args)
    {
        switch (command)
        {
            case "litmus-sb":
                {
                    int rounds = int.Parse(args[1]);
                    foreach (LitmusTest test in new LitmusTest[] { new StoreBufferingPlain(), new StoreBufferingVolatile(), new StoreBufferingFenced() })
                    {
                        Stopwatch sw = Stopwatch.StartNew();
                        long[,] o = new LitmusRunner(test).Run(rounds);
                        Console.WriteLine($"{test.Name,-24} r0=0,r1=0: {o[0, 0],9:N0}  0,1: {o[0, 1],9:N0}  1,0: {o[1, 0],9:N0}  1,1: {o[1, 1],9:N0}  ({sw.ElapsedMilliseconds} ms)");
                    }
                    return true;
                }
            case "litmus-mp":
                {
                    int rounds = int.Parse(args[1]);
                    foreach (LitmusTest test in new LitmusTest[] { new MessagePassingPlain(), new MessagePassingVolatile() })
                    {
                        Stopwatch sw = Stopwatch.StartNew();
                        long[,] o = new LitmusRunner(test).Run(rounds);
                        // r1 = ready * 2 + total
                        Console.WriteLine($"{test.Name,-24} ready=1,total=1: {o[0, 3],10:N0}  ready=0: {o[0, 0] + o[0, 1],10:N0}  ready=1,total=0: {o[0, 2],7:N0}  ({sw.ElapsedMilliseconds} ms)");
                    }
                    return true;
                }
            case "coherence":
                {
                    int writes = int.Parse(args[1]);
                    (long reads, long wentBack) = new CoherenceCheck().Run(writes, readers: 3);
                    Console.WriteLine($"writes={writes:N0} reads={reads:N0} saw the value go back={wentBack:N0}");
                    return true;
                }
        }
        return false;
    }
}
