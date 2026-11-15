using BenchmarkDotNet.Attributes;
using Bookstore.Catalog;
using Bookstore.Filter.Evaluation;
using Bookstore.Filter.Syntax;

namespace Bookstore.Filter.Benchmarks;

// The tree-walking interpreter (post 03) boxes every value it reads; the compiled delegate (post 04) does not.
[MemoryDiagnoser]
public class EvaluationBenchmarks
{
    private const string Filter = "price < 300 and author = \"Jane Austen\"";
    private readonly Book[] _books = [.. Enumerable.Range(0, 1_000).Select(i => SampleBooks.All[i % SampleBooks.All.Count])];
    private readonly Expr _ast = Parser.Parse(Filter).Root;
    private readonly Func<Book, bool> _compiled = FilterCompiler.Compile(Filter).Expression!.Compile();

    [Benchmark(Baseline = true)]
    public int TreeWalkingInterpreter() => Interpreter.Filter(_books, _ast).Count();

    [Benchmark]
    public int CompiledDelegate() => _books.Count(_compiled);
}
