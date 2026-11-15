namespace Bookstore.Api.Ai;

// A series of requests that share one cacheable prefix. Price math only; no API call.
public sealed record CacheScenario(long PrefixTokens, long SuffixTokens, long OutputTokens, int Requests, int CacheWrites);

public static class PromptCacheCostModel
{
    public static decimal WithoutCache(string model, CacheScenario s) =>
        s.Requests * ClaudePricing.EstimateUsd(model,
            new TokenUsage(s.PrefixTokens + s.SuffixTokens, s.OutputTokens, CacheRead: 0, CacheWrite: 0, Thinking: 0));

    // CacheWrites requests write the prefix (the first one, or after the entry expired); the others read it.
    public static decimal WithCache(string model, CacheScenario s)
    {
        TokenUsage write = new(s.SuffixTokens, s.OutputTokens, CacheRead: 0, CacheWrite: s.PrefixTokens, Thinking: 0);
        TokenUsage read = new(s.SuffixTokens, s.OutputTokens, CacheRead: s.PrefixTokens, CacheWrite: 0, Thinking: 0);
        return s.CacheWrites * ClaudePricing.EstimateUsd(model, write)
               + (s.Requests - s.CacheWrites) * ClaudePricing.EstimateUsd(model, read);
    }
}
