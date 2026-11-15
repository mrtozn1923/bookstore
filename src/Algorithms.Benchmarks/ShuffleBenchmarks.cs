using Algorithms.Randomness;
using BenchmarkDotNet.Attributes;

namespace Algorithms.Benchmarks;

[MemoryDiagnoser]
public class ShuffleBenchmarks
{
    private int[] _source = [];
    private int[] _buffer = [];
    private readonly Random _random = new(27);

    [Params(100_000)]
    public int N { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _source = [.. Enumerable.Range(0, N)];
        _buffer = new int[N];
    }

    [Benchmark(Baseline = true)]
    public int[] FisherYatesOurs()
    {
        _source.CopyTo(_buffer, 0);
        Shuffles.FisherYates<int>(_buffer, _random);
        return _buffer;
    }

    [Benchmark]
    public int[] RandomShuffle()
    {
        _source.CopyTo(_buffer, 0);
        _random.Shuffle(_buffer);
        return _buffer;
    }

    [Benchmark]
    public int[] OrderByGuid() => [.. _source.OrderBy(_ => Guid.NewGuid())];
}
