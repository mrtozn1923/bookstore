using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Bookstore.Catalog;

namespace Bookstore.Filter.Benchmarks;

public interface IBookRule
{
    bool Matches(Book book);
}

public sealed class InStock : IBookRule
{
    public bool Matches(Book book) => book.Stock > 0;
}

public sealed class PublishedBefore(int year) : IBookRule
{
    public bool Matches(Book book) => book.Year < year;
}

// The same interface call with Dynamic PGO on (default) and off.
[Config(typeof(PgoConfig))]
public class RuleBenchmarks
{
    private readonly Book[] _books = [.. Enumerable.Range(0, 1_000).Select(i => SampleBooks.All[i % SampleBooks.All.Count])];
    private readonly IBookRule _rule = new InStock();

    [Benchmark]
    public int CountMatches()
    {
        int count = 0;
        foreach (Book book in _books)
        {
            if (_rule.Matches(book))
                count++;
        }
        return count;
    }

    private sealed class PgoConfig : ManualConfig
    {
        public PgoConfig()
        {
            AddJob(Job.Default.WithId("PGO on"));
            AddJob(Job.Default.WithId("PGO off").WithEnvironmentVariables(new EnvironmentVariable("DOTNET_TieredPGO", "0")));
        }
    }
}
