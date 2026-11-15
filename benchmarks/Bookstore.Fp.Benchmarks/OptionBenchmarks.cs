using BenchmarkDotNet.Attributes;
using Bookstore.Ordering.Functional;
using Bookstore.Ordering.Pricing;

namespace Bookstore.Fp.Benchmarks;

[MemoryDiagnoser]
public class OptionBenchmarks
{
    private static readonly TimeSpan Istanbul = TimeSpan.FromHours(3);
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 12, 0, 0, Istanbul);

    private readonly Dictionary<string, Campaign> _campaigns = new()
    {
        ["AUTUMN10"] = new("AUTUMN10", 0.10m, new(2026, 11, 1, 0, 0, 0, Istanbul), new(2026, 12, 1, 0, 0, 0, Istanbul))
    };

    [Params(" autumn10 ")]
    public string Code { get; set; } = "";

    [GlobalSetup]
    public void Setup()
    {
        if (StructOption() != NullChecks() || StructOptionInline() != NullChecks() || ClassOption() != NullChecks())
            throw new InvalidOperationException("The implementations disagree.");
    }

    [Benchmark(Baseline = true)]
    public decimal NullChecks()
    {
        string? normalized = Code?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(normalized))
            return 0m;
        if (!_campaigns.TryGetValue(normalized, out Campaign? campaign) || !campaign.IsActiveAt(Now))
            return 0m;
        return Math.Round(249.40m * campaign.Rate, 2, MidpointRounding.AwayFromZero);
    }

    [Benchmark]
    public decimal StructOption() => CouponLookup.CouponDiscount(249.40m, Code, _campaigns, Now);

    [Benchmark]
    public decimal StructOptionInline() =>
        Code.ToOption()
            .Map(text => text.Trim().ToUpperInvariant())
            .Where(text => text.Length > 0)
            .Bind(code => _campaigns.TryGetValue(code, out Campaign? c) ? Option.Some(c) : Option<Campaign>.None)
            .Where(campaign => campaign.IsActiveAt(Now))
            .Map(campaign => Math.Round(249.40m * campaign.Rate, 2, MidpointRounding.AwayFromZero))
            .GetValueOrDefault(0m);

    [Benchmark]
    public decimal ClassOption() =>
        ClassOption<string>.From(Code)
            .Map(text => text.Trim().ToUpperInvariant())
            .Where(text => text.Length > 0)
            .Bind(code => _campaigns.TryGetValue(code, out Campaign? c) ? ClassOption<Campaign>.Some(c) : ClassOption<Campaign>.None)
            .Where(campaign => campaign.IsActiveAt(Now))
            .Map(campaign => Math.Round(249.40m * campaign.Rate, 2, MidpointRounding.AwayFromZero))
            .GetValueOrDefault(0m);
}

// The same Option as a class: every Some is a heap allocation
public sealed class ClassOption<T>
{
    private readonly T? _value;

    private ClassOption(T? value, bool isSome) => (_value, IsSome) = (value, isSome);

    public bool IsSome { get; }

    public static ClassOption<T> None { get; } = new(default, false);

    public static ClassOption<T> Some(T value) => new(value, true);

    public static ClassOption<T> From(T? value) => value is null ? None : Some(value);

    public ClassOption<TOut> Map<TOut>(Func<T, TOut> map) => IsSome ? ClassOption<TOut>.Some(map(_value!)) : ClassOption<TOut>.None;

    public ClassOption<TOut> Bind<TOut>(Func<T, ClassOption<TOut>> bind) => IsSome ? bind(_value!) : ClassOption<TOut>.None;

    public ClassOption<T> Where(Func<T, bool> predicate) => IsSome && predicate(_value!) ? this : None;

    public T GetValueOrDefault(T fallback) => IsSome ? _value! : fallback;
}
