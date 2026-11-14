using System.Diagnostics;
using Bookstore.ConcurrencyLab.Deadlocks;

namespace Bookstore.ConcurrencyLab.Commands;

// Post 04: deadlock, livelock, starvation
public static class DeadlockCommands
{
    private const string Dune = "9780441013593";

    public static bool Run(string command, string[] args)
    {
        switch (command)
        {
            case "transfer":
                {
                    string mode = args[1];
                    int transfers = int.Parse(args[2]);
                    Warehouse istanbul = new(1, "Istanbul"), ankara = new(2, "Ankara");
                    istanbul.Stock[Dune] = 1_000_000;
                    ankara.Stock[Dune] = 1_000_000;
                    int failed = 0;
                    void Run(Warehouse from, Warehouse to)
                    {
                        for (int i = 0; i < transfers; i++)
                        {
                            switch (mode)
                            {
                                case "naive": StockTransfer.Move(from, to, Dune, 1); break;
                                case "ordered": StockTransfer.MoveOrdered(from, to, Dune, 1); break;
                                case "timeout":
                                    while (!StockTransfer.TryMove(from, to, Dune, 1, TimeSpan.FromMilliseconds(1)))
                                        Interlocked.Increment(ref failed);
                                    break;
                                case "timeout-backoff":
                                    while (!StockTransfer.TryMove(from, to, Dune, 1, TimeSpan.FromMilliseconds(1)))
                                    {
                                        Interlocked.Increment(ref failed);
                                        Thread.Sleep(Random.Shared.Next(0, 3));
                                    }
                                    break;
                            }
                        }
                    }
                    Stopwatch sw = Stopwatch.StartNew();
                    Thread east = new(() => Run(istanbul, ankara)) { IsBackground = true };
                    Thread west = new(() => Run(ankara, istanbul)) { IsBackground = true };
                    east.Start(); west.Start();
                    bool finished = east.Join(TimeSpan.FromSeconds(5)) & west.Join(TimeSpan.FromSeconds(5));
                    if (finished)
                    {
                        lock (istanbul.Gate) lock (ankara.Gate)
                            Console.WriteLine($"{mode}: finished in {sw.ElapsedMilliseconds} ms, total stock={istanbul.Stock[Dune] + ankara.Stock[Dune]:N0}, timeouts={failed:N0}");
                    }
                    else
                    {
                        Console.WriteLine($"{mode}: not finished after 5 s; timeouts={Volatile.Read(ref failed):N0}, east={east.ThreadState}, west={west.ThreadState}");
                    }
                    return true;
                }
            case "philosophers":
                {
                    foreach (bool ordered in new[] { false, true })
                    {
                        Stopwatch sw = Stopwatch.StartNew();
                        bool done = Philosophers.Dine(seats: 5, meals: 100_000, ordered, TimeSpan.FromSeconds(5));
                        Console.WriteLine($"{(ordered ? "ordered forks" : "left then right"),-16} {(done ? $"all ate in {sw.ElapsedMilliseconds} ms" : "table froze (deadlock)")}");
                    }
                    return true;
                }
            case "livelock":
                {
                    int transfers = int.Parse(args[1]);
                    foreach (bool backoff in new[] { false, true })
                    {
                        Stopwatch sw = Stopwatch.StartNew();
                        long retries = PoliteTransfers.Retries(transfers, backoff);
                        Console.WriteLine($"{(backoff ? "random backoff" : "retry at once"),-15} transfers={2 * transfers:N0} retries={retries:N0} time={sw.ElapsedMilliseconds} ms");
                    }
                    return true;
                }
            case "wait-for":
                {
                    WaitForGraph graph = new();
                    graph.AddWait("T1 (Istanbul->Ankara)", "T2 (Ankara->Istanbul)");
                    graph.AddWait("T2 (Ankara->Istanbul)", "T1 (Istanbul->Ankara)");
                    graph.AddWait("T3 (report)", "T1 (Istanbul->Ankara)");
                    Console.WriteLine("cycle: " + string.Join(" -> ", graph.FindCycle()));
                    return true;
                }
        }
        return false;
    }
}
