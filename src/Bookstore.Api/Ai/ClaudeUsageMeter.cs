using System.Diagnostics.Metrics;

namespace Bookstore.Api.Ai;

// Per-feature token and cost counters next to the SDK's spans; the bill is checked against the Console cost page.
public sealed class ClaudeUsageMeter : IDisposable
{
    public const string MeterName = "Bookstore.Claude";

    private readonly Meter _meter;
    private readonly Counter<long> _tokens;
    private readonly Counter<double> _cost;

    public ClaudeUsageMeter(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);
        _tokens = _meter.CreateCounter<long>("bookstore.claude.tokens", unit: "{token}");
        _cost = _meter.CreateCounter<double>("bookstore.claude.cost", unit: "USD");
    }

    public decimal Record(string feature, string model, TokenUsage usage)
    {
        KeyValuePair<string, object?> featureTag = new("feature", feature);
        KeyValuePair<string, object?> modelTag = new("model", model);
        _tokens.Add(usage.Input, featureTag, modelTag, new("type", "input"));
        _tokens.Add(usage.CacheRead, featureTag, modelTag, new("type", "cache_read"));
        _tokens.Add(usage.CacheWrite, featureTag, modelTag, new("type", "cache_write"));
        _tokens.Add(usage.Output, featureTag, modelTag, new("type", "output"));

        decimal usd = ClaudePricing.KnownModels.Contains(model) ? ClaudePricing.EstimateUsd(model, usage) : 0m;
        _cost.Add((double)usd, featureTag, modelTag);
        return usd;
    }

    public void Dispose() => _meter.Dispose();
}
