using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using Bookstore.Catalog;

namespace Bookstore.Filter.Benchmarks;

// What Native AOT changes for the filter language: Compile() falls back to the expression interpreter.
// preferInterpretation: true selects the same interpreter on a JIT runtime.
[MemoryDiagnoser]
public class CompileBenchmarks
{
    private const string Filter = "price < 300 and author = \"Jane Austen\"";
    private readonly Book[] _books = [.. Enumerable.Range(0, 1_000).Select(i => SampleBooks.All[i % SampleBooks.All.Count])];
    private readonly Expression<Func<Book, bool>> _expression = FilterCompiler.Compile(Filter).Expression!;
    private Func<Book, bool> _compiled = null!;
    private Func<Book, bool> _interpreted = null!;

    [GlobalSetup]
    public void Setup()
    {
        _compiled = _expression.Compile();
        _interpreted = _expression.Compile(preferInterpretation: true);
    }

    [Benchmark]
    public Func<Book, bool> CompileToIL() => _expression.Compile();

    [Benchmark]
    public Func<Book, bool> CompileForInterpreter() => _expression.Compile(preferInterpretation: true);

    [Benchmark]
    public int RunCompiled() => Count(_compiled);

    [Benchmark]
    public int RunInterpreted() => Count(_interpreted);

    private int Count(Func<Book, bool> predicate)
    {
        int count = 0;
        foreach (Book book in _books)
        {
            if (predicate(book))
                count++;
        }
        return count;
    }
}
