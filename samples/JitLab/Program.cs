using System.Diagnostics;
using System.Globalization;
using Bookstore.Catalog;
using Bookstore.JitLab;

Book[] books = [.. SampleBooks.All];

switch (args.FirstOrDefault())
{
    case "tiers":
        Tiers(books);
        break;
    case "pgo":
        Pgo(books);
        break;
    case "alloc":
        Alloc(books);
        break;
    case "warmup":
        Warmup(books);
        break;
    default:
        Console.Error.WriteLine("usage: JitLab tiers|pgo|alloc|warmup");
        return 2;
}
return 0;

static void Tiers(Book[] books)
{
    int total = 0;
    // a single long-running loop in a method that is called only once
    for (int i = 0; i < 2_000_000; i++)
        total += Lab.CountInStock(books) + Lab.CountInStock(books, 3);
    Console.WriteLine($"total: {total}");
}

static void Pgo(Book[] books)
{
    IBookRule inStock = new InStock();
    IBookRule old = new PublishedBefore(1900);
    int total = 0;
    for (int round = 0; round < 50; round++)
    {
        // 9 calls out of 10 go to InStock
        for (int i = 0; i < 1_000; i++)
            total += Lab.CountMatches(books, i % 10 == 0 ? old : inStock);
        Thread.Sleep(10);
    }
    Console.WriteLine($"total: {total}");
}

static void Alloc(Book[] books)
{
    // warm up: let every method reach Tier1 before measuring
    for (int round = 0; round < 10; round++)
    {
        for (int i = 0; i < 100; i++)
            Run(books);
        Thread.Sleep(100);
    }

    Console.WriteLine($".NET {Environment.Version}");
    Measure("ShippingDays", () => Lab.ShippingDays(7));
    Measure("CountCheaperThan", () => Lab.CountCheaperThan(books, 250m));
    Measure("CountInStockSeq", () => Lab.CountInStockSeq(books));
}

static void Warmup(Book[] books)
{
    IBookRule rule = new InStock();
    for (int round = 1; round <= 8; round++)
    {
        long start = Stopwatch.GetTimestamp();
        for (int i = 0; i < 2_000; i++)
            Lab.CountMatches(books, rule);
        double microseconds = Stopwatch.GetElapsedTime(start).TotalMicroseconds;
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"round {round}: {microseconds,8:F0} us"));
        Thread.Sleep(50);
    }
}

static void Run(Book[] books)
{
    Lab.ShippingDays(7);
    Lab.CountCheaperThan(books, 250m);
    Lab.CountInStockSeq(books);
}

static void Measure(string name, Func<int> action)
{
    const int calls = 10_000;
    long before = GC.GetAllocatedBytesForCurrentThread();
    for (int i = 0; i < calls; i++)
        action();
    long perCall = (GC.GetAllocatedBytesForCurrentThread() - before) / calls;
    Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{name,-18} {perCall,4} bytes/call"));
}
