namespace Bookstore.Api.Ai;

// USD per million tokens, from platform.claude.com/docs/en/about-claude/pricing (checked 2026-11-15).
public sealed record ModelPrice(decimal Input, decimal CacheWrite5m, decimal CacheWrite1h, decimal CacheRead, decimal Output);

public static class ClaudePricing
{
    private const long HaikuLongPromptThreshold = 100_000;

    private static readonly Dictionary<string, ModelPrice> Prices = new()
    {
        ["claude-fable-5-2"] = new(8.00m, 10.00m, 16.00m, 0.20m, 40.00m),
        ["claude-opus-5-5"] = new(4.00m, 5.00m, 8.00m, 0.20m, 20.00m),
        ["claude-sonnet-5-5"] = new(2.00m, 2.50m, 4.00m, 0.10m, 10.00m),
        ["claude-haiku-5-5"] = new(0.10m, 0.125m, 0.20m, 0.01m, 0.50m),
    };

    // Haiku 5.5 has a higher price when the prompt (input + cache read + cache write) is over 100,000 tokens.
    private static readonly ModelPrice HaikuLongPrompt = new(0.50m, 0.625m, 1.00m, 0.05m, 2.50m);

    public static IReadOnlyCollection<string> KnownModels => Prices.Keys;

    // Message Batches: every token type is billed at 50% of the standard price.
    public const decimal BatchMultiplier = 0.5m;

    public static decimal EstimateUsd(string model, TokenUsage usage, bool batch = false)
    {
        if (!Prices.TryGetValue(model, out ModelPrice? price))
        {
            throw new ArgumentException($"No price for model '{model}'.", nameof(model));
        }

        long promptTokens = usage.Input + usage.CacheRead + usage.CacheWrite;
        if (model == "claude-haiku-5-5" && promptTokens > HaikuLongPromptThreshold)
        {
            price = HaikuLongPrompt;
        }

        // Thinking tokens are already inside Output; they have no separate price.
        // CacheWrite holds every cache write; CacheWrite1h is the part written with the 1-hour TTL.
        long write5m = usage.CacheWrite - usage.CacheWrite1h;
        decimal total = usage.Input * price.Input
                        + write5m * price.CacheWrite5m
                        + usage.CacheWrite1h * price.CacheWrite1h
                        + usage.CacheRead * price.CacheRead
                        + usage.Output * price.Output;
        return (batch ? total * BatchMultiplier : total) / 1_000_000m;
    }
}
