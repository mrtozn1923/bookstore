using System.Diagnostics;
using System.Globalization;
using Bookstore.Catalog;
using Bookstore.Filter;
using Bookstore.Filter.Evaluation;
using Bookstore.Filter.Syntax;
using Bookstore.GcLab;

const string Filter = "price < 300 and author = \"Jane Austen\"";

switch (args)
{
    case ["gens"]:
        Generations();
        break;
    case ["filter", string mode, string seconds]:
        FilterLoad(mode, int.Parse(seconds, CultureInfo.InvariantCulture));
        break;
    case ["memory"]:
        GarbageOrLeak();
        break;
    case ["barrier"]:
        Barrier();
        break;
    case ["modes", string threads]:
        Modes(int.Parse(threads, CultureInfo.InvariantCulture));
        break;
    default:
        Console.Error.WriteLine("usage: GcLab gens | filter interpreter|compiled <seconds> | memory | barrier | modes <threads>");
        return 2;
}
return 0;

static void Generations()
{
    Book book = new("9780441172719", "Dune", "Frank Herbert", 349.90m, 1965, 12);
    Console.WriteLine($"new Book:               gen {GC.GetGeneration(book)}");
    // GC.Collect only to show promotion; never call it like this in an app
    GC.Collect(0);
    Console.WriteLine($"after a gen0 GC:        gen {GC.GetGeneration(book)}");
    GC.Collect(1);
    Console.WriteLine($"after a gen1 GC:        gen {GC.GetGeneration(book)}");

    byte[] thumbnail = new byte[84_000];
    byte[] cover = new byte[85_000];
    byte[] pinned = GC.AllocateArray<byte>(4_096, pinned: true);
    Console.WriteLine($"byte[84_000] thumbnail: gen {GC.GetGeneration(thumbnail)}");
    Console.WriteLine($"byte[85_000] cover:     gen {GC.GetGeneration(cover)} (LOH)");
    Console.WriteLine($"pinned byte[4_096]:     gen {GC.GetGeneration(pinned)} (POH)");
}

static void FilterLoad(string mode, int seconds)
{
    Book[] catalog = LabCatalog.Generate(100_000);
    Expr ast = Parser.Parse(Filter).Root;
    Func<Book, bool> compiled = FilterCompiler.Compile(Filter).Expression!.Compile();
    Func<int> pass = mode == "interpreter"
        ? () => Interpreter.Filter(catalog, ast).Count()
        : () => catalog.Count(compiled);

    GcSnapshot before = GcSnapshot.Take();
    long start = Stopwatch.GetTimestamp();
    int passes = 0;
    while (Stopwatch.GetElapsedTime(start).TotalSeconds < seconds)
    {
        pass();
        passes++;
    }
    TimeSpan elapsed = Stopwatch.GetElapsedTime(start);
    Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
        $"{mode,-11} {passes / elapsed.TotalSeconds,6:F1} passes/s | {GcSnapshot.Take().Since(before)}"));
}

static void GarbageOrLeak()
{
    Report("start");

    // 200 MB of short-lived small objects: gen0 collects them cheaply
    for (int i = 0; i < 200_000; i++)
        Sink.Last = new byte[1_000];
    Report("200 MB in 1 KB arrays");

    // the same 200 MB as cover images: every array goes to the LOH, which only a gen2 GC collects
    for (int i = 0; i < 200; i++)
        Sink.Last = new byte[1_000_000];
    Report("200 MB in 1 MB arrays");

    // an unbounded cache: every distinct filter text keeps a compiled delegate alive
    Dictionary<string, Func<Book, bool>> cache = [];
    for (int i = 1; i <= 10_000; i++)
    {
        cache[$"price < {i}"] = FilterCompiler.Compile($"price < {i}").Expression!.Compile();
        if (i % 5_000 == 0)
        {
            GC.GetTotalMemory(forceFullCollection: true);
            Report($"{i} cached filters, full GC");
        }
    }
    GC.KeepAlive(cache);
}

static void Report(string label)
{
    using Process process = Process.GetCurrentProcess();
    Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
        $"{label,-28} heap {GC.GetTotalMemory(false) / 1_048_576,4} MB | working set {process.WorkingSet64 / 1_048_576,4} MB | GCs gen0 {GC.CollectionCount(0),3} gen2 {GC.CollectionCount(2),3}"));
}

static void Barrier()
{
    Shelf shelf = new();
    Book[] catalog = LabCatalog.Generate(100_000);
    for (int round = 0; round < 5; round++)
    {
        foreach (Book book in catalog)
            shelf.Put(book);
        // let tiered compilation produce the optimized Put
        Thread.Sleep(200);
    }
    Console.WriteLine($"{shelf.Count} books, latest: {shelf.Latest?.Title}");
}

static void Modes(int threads)
{
    IReadOnlyDictionary<string, object> config = GC.GetConfigurationVariables();
    Book[] catalog = LabCatalog.Generate(10_000);
    Expr ast = Parser.Parse(Filter).Root;
    GcSnapshot before = GcSnapshot.Take();
    long start = Stopwatch.GetTimestamp();
    long passes = 0;
    long peakWorkingSet = 0;
    using Process process = Process.GetCurrentProcess();
    Thread[] workers = [.. Enumerable.Range(0, threads).Select(_ => new Thread(() =>
    {
        while (Stopwatch.GetElapsedTime(start).TotalSeconds < 5)
        {
            Interpreter.Filter(catalog, ast).Count();
            Interlocked.Increment(ref passes);
        }
    }))];
    foreach (Thread worker in workers)
        worker.Start();
    while (workers.Any(w => w.IsAlive))
    {
        process.Refresh();
        peakWorkingSet = Math.Max(peakWorkingSet, process.WorkingSet64);
        Thread.Sleep(50);
    }
    Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
        $"threads={threads} server={config["ServerGC"]} datas={config["GCDynamicAdaptationMode"]} | {passes / 5.0,6:F0} passes/s | peak working set {peakWorkingSet / 1_048_576,4} MB | {GcSnapshot.Take().Since(before)}"));
}

// the JIT may delete an allocation nobody reads; writing it to a static field keeps it real
static class Sink
{
    public static byte[]? Last;
}

readonly record struct GcSnapshot(int Gen0, int Gen1, int Gen2, long Allocated, TimeSpan Pause)
{
    public static GcSnapshot Take() => new(GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2),
        GC.GetTotalAllocatedBytes(), GC.GetTotalPauseDuration());

    public string Since(GcSnapshot before) => string.Create(CultureInfo.InvariantCulture,
        $"allocated {(Allocated - before.Allocated) / 1_048_576,6} MB | gen0 {Gen0 - before.Gen0,4} gen1 {Gen1 - before.Gen1,3} gen2 {Gen2 - before.Gen2,2} | pause {(Pause - before.Pause).TotalMilliseconds,6:F1} ms");
}
