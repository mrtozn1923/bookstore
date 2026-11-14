using BenchmarkDotNet.Attributes;
using Bookstore.ConcurrencyLab.LockFree;

namespace Bookstore.ConcurrencyLab.Benchmarks;

// 10,000,000 increments shared by N threads; Mean is per increment thanks to OperationsPerInvoke
[SimpleJob(warmupCount: 3, iterationCount: 15)]
public class CounterBenchmarks
{
    private const int Total = 10_000_000;
    private readonly Lock _gate = new();
    private long _plain;
    private long _interlocked;
    private LockFreeCounter _casCounter = new();

    [Params(1, 2, 4, 8)]
    public int Threads { get; set; }

    [IterationSetup]
    public void Reset() => _casCounter = new LockFreeCounter();

    [Benchmark(Baseline = true, OperationsPerInvoke = Total)]
    public void Locked() => RunOnThreads(() =>
    {
        for (int i = 0; i < Total / Threads; i++)
            lock (_gate)
                _plain++;
    });

    [Benchmark(OperationsPerInvoke = Total)]
    public void InterlockedIncrement() => RunOnThreads(() =>
    {
        for (int i = 0; i < Total / Threads; i++)
            Interlocked.Increment(ref _interlocked);
    });

    [Benchmark(OperationsPerInvoke = Total)]
    public void CasLoop() => RunOnThreads(() =>
    {
        for (int i = 0; i < Total / Threads; i++)
            _casCounter.Add(1);
    });

    private void RunOnThreads(Action body)
    {
        Thread[] threads = [.. Enumerable.Range(0, Threads).Select(_ => new Thread(() => body()))];
        foreach (Thread t in threads) t.Start();
        foreach (Thread t in threads) t.Join();
    }
}
