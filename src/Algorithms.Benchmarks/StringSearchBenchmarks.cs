using System.Buffers;
using Algorithms.Strings;
using BenchmarkDotNet.Attributes;

namespace Algorithms.Benchmarks;

[MemoryDiagnoser]
public class SingleWordSearchBenchmarks
{
    private string _text = "";
    private const string Pattern = "Middlemarch";

    [GlobalSetup]
    public void Setup() => _text = TextFactory.Words(100_000, seed: 24) + " " + Pattern; // the only match is at the end

    [Benchmark(Baseline = true)]
    public int Naive() => NaiveSearch.IndexOf(_text, Pattern, out _);

    [Benchmark]
    public int Kmp() => Strings.Kmp.FindAll(_text, Pattern).FirstOrDefault(-1);

    [Benchmark]
    public int IndexOfOrdinal() => _text.IndexOf(Pattern, StringComparison.Ordinal);

    [Benchmark]
    public int IndexOfCurrentCulture() => _text.IndexOf(Pattern, StringComparison.CurrentCulture);
}

[MemoryDiagnoser]
public class BannedWordsBenchmarks
{
    private string[] _reviews = [];
    private string[] _banned = [];
    private AhoCorasick _ac = null!;
    private SearchValues<string> _sv = null!;

    [Params(10, 500)]
    public int BannedCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Random random = new(24);
        _reviews = [.. Enumerable.Range(0, 1_000).Select(i => TextFactory.Words(500, seed: i))];
        _banned = [.. Enumerable.Range(0, BannedCount).Select(_ => new string([.. Enumerable.Range(0, random.Next(5, 10)).Select(_ => (char)('a' + random.Next(26)))]))];
        _ac = new AhoCorasick(_banned);
        _sv = SearchValues.Create(_banned, StringComparison.Ordinal);
    }

    [Benchmark(Baseline = true)]
    public int LoopContains() => _reviews.Count(r => _banned.Any(w => r.Contains(w, StringComparison.Ordinal)));

    [Benchmark]
    public int AhoCorasickOurs() => _reviews.Count(r => _ac.ContainsAny(r));

    [Benchmark]
    public int SearchValuesContainsAny() => _reviews.Count(r => r.AsSpan().ContainsAny(_sv));
}

public static class TextFactory
{
    private static readonly string[] Vocabulary =
    [
        "the", "book", "story", "author", "reader", "chapter", "novel", "classic", "dune", "emma", "hobbit",
        "desert", "family", "letter", "great", "slow", "start", "ending", "character", "world", "paper", "cover",
    ];

    public static string Words(int length, int seed)
    {
        Random random = new(seed);
        System.Text.StringBuilder sb = new(length + 16);
        while (sb.Length < length) sb.Append(Vocabulary[random.Next(Vocabulary.Length)]).Append(' ');
        return sb.ToString(0, length);
    }
}
