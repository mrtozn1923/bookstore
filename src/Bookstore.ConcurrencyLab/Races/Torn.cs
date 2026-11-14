namespace Bookstore.ConcurrencyLab.Races;

public readonly record struct PriceRange(long Min, long Max);

// 32 bytes: list price and campaign price of a book
public readonly record struct ShelfPrice(decimal List, decimal Campaign);

public sealed class TornReads
{
    private decimal _price;
    private PriceRange _range;
    private ShelfPrice _shelf;

    // Writer alternates between two valid values; reader counts values that were never written
    public static long CountTornDecimals(int writes)
    {
        TornReads box = new() { _price = 1.00m };
        long torn = 0;
        bool done = false;
        Thread reader = new(() =>
        {
            while (!Volatile.Read(ref done))
            {
                decimal seen = box._price;
                if (seen != 1.00m && seen != 99_999.99m)
                    torn++;
            }
        });
        reader.Start();
        for (int i = 0; i < writes; i++)
            box._price = i % 2 == 0 ? 99_999.99m : 1.00m;
        Volatile.Write(ref done, true);
        reader.Join();
        return torn;
    }

    // Invariant Min <= Max holds for every write, but not for every read
    public static long CountBrokenRanges(int writes)
    {
        TornReads box = new() { _range = new PriceRange(100, 200) };
        long broken = 0;
        bool done = false;
        Thread reader = new(() =>
        {
            while (!Volatile.Read(ref done))
            {
                PriceRange seen = box._range;
                if (seen.Min > seen.Max)
                    broken++;
            }
        });
        reader.Start();
        for (int i = 0; i < writes; i++)
            box._range = i % 2 == 0 ? new PriceRange(5_000, 9_000) : new PriceRange(100, 200);
        Volatile.Write(ref done, true);
        reader.Join();
        return broken;
    }

    // Invariant Campaign <= List holds for every write
    public static long CountBrokenShelfPrices(int writes)
    {
        TornReads box = new() { _shelf = new ShelfPrice(450.00m, 399.90m) };
        long broken = 0;
        bool done = false;
        Thread reader = new(() =>
        {
            while (!Volatile.Read(ref done))
            {
                ShelfPrice seen = box._shelf;
                if (seen.Campaign > seen.List)
                    broken++;
            }
        });
        reader.Start();
        for (int i = 0; i < writes; i++)
            box._shelf = i % 2 == 0 ? new ShelfPrice(1_200.00m, 999.90m) : new ShelfPrice(450.00m, 399.90m);
        Volatile.Write(ref done, true);
        reader.Join();
        return broken;
    }
}
