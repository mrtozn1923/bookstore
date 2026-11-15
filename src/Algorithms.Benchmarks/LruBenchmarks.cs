using Algorithms.Caching;
using BenchmarkDotNet.Attributes;
using BitFaster.Caching.Lru;

namespace Algorithms.Benchmarks;

[MemoryDiagnoser]
public class LruBenchmarks
{
    private int[] _keys = [];

    [Params(1_000, 10_000)]
    public int Capacity { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Random random = new(23);
        _keys = [.. Enumerable.Range(0, 100_000).Select(_ => random.Next(Capacity * 2))]; // about 50% hits
    }

    [Benchmark(Baseline = true)]
    public int DictionaryPlusLinkedList()
    {
        LruCache<int, int> cache = new(Capacity);
        int hits = 0;
        foreach (int key in _keys)
        {
            if (cache.TryGet(key, out _)) hits++;
            else cache.Put(key, key);
        }

        return hits;
    }

    [Benchmark]
    public int DictionaryPlusList()
    {
        NaiveLruCache<int, int> cache = new(Capacity);
        int hits = 0;
        foreach (int key in _keys)
        {
            if (cache.TryGet(key, out _)) hits++;
            else cache.Put(key, key);
        }

        return hits;
    }

    [Benchmark]
    public int BitFasterConcurrentLru()
    {
        ConcurrentLru<int, int> cache = new(Capacity);
        int hits = 0;
        foreach (int key in _keys)
        {
            if (cache.TryGet(key, out _)) hits++;
            else cache.AddOrUpdate(key, key);
        }

        return hits;
    }
}
