using Bookstore.ConcurrencyLab.Races;

namespace Bookstore.ConcurrencyLab.Commands;

// Post 02: race conditions and data races
public static class RaceCommands
{
    public static bool Run(string command, string[] args)
    {
        switch (command)
        {
            case "lost-update":
                {
                    int threads = int.Parse(args[1]), perThread = int.Parse(args[2]);
                    SoldCounter plain = new(), atomic = new();
                    RaceRunner.RunTogether(threads, _ => { for (int i = 0; i < perThread; i++) plain.RecordSale(); });
                    RaceRunner.RunTogether(threads, _ => { for (int i = 0; i < perThread; i++) atomic.RecordSaleAtomic(); });
                    Console.WriteLine($"expected={threads * perThread:N0} plain={plain.Sold:N0} atomic={atomic.Sold:N0}");
                    return true;
                }
            case "oversell":
                {
                    int rounds = int.Parse(args[1]);
                    int oversold = 0, worst = 0, dictOversold = 0;
                    for (int r = 0; r < rounds; r++)
                    {
                        StockItem dune = new("9780441013593", 100);
                        int reserved = 0;
                        RaceRunner.RunTogether(8, _ =>
                        {
                            for (int i = 0; i < 50; i++)
                                if (dune.TryReserve()) Interlocked.Increment(ref reserved);
                        });
                        if (reserved > 100) { oversold++; worst = Math.Max(worst, reserved); }

                        CatalogStock stock = new();
                        stock.Set("9780441013593", 100);
                        int dictReserved = 0;
                        RaceRunner.RunTogether(8, _ =>
                        {
                            for (int i = 0; i < 50; i++)
                                if (stock.TryReserve("9780441013593")) Interlocked.Increment(ref dictReserved);
                        });
                        if (dictReserved > 100) dictOversold++;
                    }
                    Console.WriteLine($"rounds={rounds} StockItem oversold in {oversold} rounds (worst: {worst} sold of 100)");
                    Console.WriteLine($"rounds={rounds} ConcurrentDictionary oversold in {dictOversold} rounds");
                    return true;
                }
            case "init-order":
                {
                    int runs = int.Parse(args[1]), failures = 0;
                    for (int i = 0; i < runs; i++)
                    {
                        ShipmentTracker tracker = new();
                        tracker.Wait();
                        if (tracker.WorkerError is not null) failures++;
                    }
                    Console.WriteLine($"runs={runs} worker saw an uninitialized field in {failures} runs");
                    return true;
                }
            case "lazy-init":
                {
                    int rounds = int.Parse(args[1]), duplicated = 0;
                    for (int r = 0; r < rounds; r++)
                    {
                        IndexHolder.Reset();
                        SearchIndex.Created = 0;
                        RaceRunner.RunTogether(4, _ => IndexHolder.Get());
                        if (SearchIndex.Created > 1) duplicated++;
                    }
                    Console.WriteLine($"rounds={rounds} index created more than once in {duplicated} rounds");
                    return true;
                }
            case "torn":
                {
                    int writes = int.Parse(args[1]);
                    Console.WriteLine($"decimal: {TornReads.CountTornDecimals(writes):N0} torn reads in {writes:N0} writes");
                    Console.WriteLine($"PriceRange: {TornReads.CountBrokenRanges(writes):N0} reads with Min > Max in {writes:N0} writes");
                    Console.WriteLine($"ShelfPrice: {TornReads.CountBrokenShelfPrices(writes):N0} reads with Campaign > List in {writes:N0} writes");
                    return true;
                }
        }
        return false;
    }
}
