using BenchmarkDotNet.Attributes;

namespace Bookstore.OsLab.Benchmarks;

[MemoryDiagnoser]
public class OrderLogBenchmarks
{
    private string _path = "";

    [Params(10_000)]
    public int Lines { get; set; }

    [IterationSetup]
    public void Setup()
    {
        _path = Path.Combine(Path.GetTempPath(), "oslab-bench.log");
        File.Delete(_path);
    }

    [Benchmark(Baseline = true)]
    public void Naive() => OrderLog.WriteNaive(_path, Lines);

    [Benchmark]
    public void Buffered() => OrderLog.WriteBuffered(_path, Lines);
}
