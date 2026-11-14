#:package BenchmarkDotNet@0.15.8
#:property Configuration=Release
#:property PublishAot=false

using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

BenchmarkRunner.Run<IsbnBenchmarks>(args: args);

[MemoryDiagnoser]
[InProcess]
public partial class IsbnBenchmarks
{
    private const string Pattern = @"^97[89]-[0-9]{1,5}-[0-9]{1,7}-[0-9]{1,7}-[0-9]\z";
    private const string Input = "978-0-441-17271-9";

    private static readonly Regex Interpreted = new(Pattern);
    private static readonly Regex Compiled = new(Pattern, RegexOptions.Compiled);
    private static readonly Regex NonBacktracking = new(Pattern, RegexOptions.NonBacktracking);

    [GeneratedRegex(Pattern)]
    private static partial Regex Generated { get; }

    // construction on every call
    [Benchmark] public bool NewInterpreted() => new Regex(Pattern).IsMatch(Input);
    [Benchmark] public bool NewCompiled() => new Regex(Pattern, RegexOptions.Compiled).IsMatch(Input);
    [Benchmark] public bool StaticMethod() => Regex.IsMatch(Input, Pattern);

    // one instance, reused
    [Benchmark(Baseline = true)] public bool Interpreter() => Interpreted.IsMatch(Input);
    [Benchmark] public bool CompiledOption() => Compiled.IsMatch(Input);
    [Benchmark] public bool NonBacktrackingOption() => NonBacktracking.IsMatch(Input);
    [Benchmark] public bool SourceGenerated() => Generated.IsMatch(Input);
}
