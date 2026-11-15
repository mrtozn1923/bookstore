#:property PublishAot=false
// Estimates the GitHub AI Credits of one agent session from per-token prices.
// Prices: docs.github.com "Models and pricing for GitHub Copilot", USD per 1M tokens, checked 2026-11-15.
// Run: dotnet run eng/ai-credits.cs -- [steps]
using System.Globalization;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

int steps = args.Length > 0 ? int.Parse(args[0]) : 30;

// The agent loop resends the whole conversation on every call.
const int Prefix = 20_000;       // system prompt, tool definitions, custom instructions
const int ToolResult = 2_500;    // file read, build or test output added per step
const int Output = 600;          // reasoning + tool call written by the model per step

ModelPrice[] models =
[
    new("GPT-6 Luna", Input: 0.10m, CachedInput: 0.01m, CacheWrite: 0.125m, OutputPrice: 0.50m),
    new("Claude Sonnet 5.5", Input: 2.00m, CachedInput: 0.10m, CacheWrite: 2.50m, OutputPrice: 10.00m),
    new("Claude Opus 5.5", Input: 4.00m, CachedInput: 0.20m, CacheWrite: 5.00m, OutputPrice: 20.00m),
];

long cached = 0, written = 0, output = 0;
int context = Prefix;
for (int step = 1; step <= steps; step++)
{
    int newTokens = step == 1 ? Prefix : Output + ToolResult;
    cached += context - newTokens;   // already in the prompt cache
    written += newTokens;            // appended on this call, cached for the next one
    output += Output;
    context += Output + ToolResult;
}

Console.WriteLine($"Steps: {steps}, last context: {context:N0} tokens");
Console.WriteLine($"Input sent: {cached + written:N0} (cached {cached:N0}, written {written:N0}), output: {output:N0}");
Console.WriteLine();
Console.WriteLine($"{"Model",-20}{"USD",10}{"AI credits",12}{"Sessions in Pro (1,500)",26}");
foreach (ModelPrice m in models)
{
    decimal usd = (cached * m.CachedInput + written * m.CacheWrite + output * m.OutputPrice) / 1_000_000m;
    decimal credits = usd * 100m;   // 1 AI credit = 0.01 USD
    Console.WriteLine($"{m.Name,-20}{usd,10:F2}{credits,12:F0}{Math.Floor(1_500m / credits),26:F0}");
}

// Input is listed for reference: in this model every new token is written to the prompt cache.
record ModelPrice(string Name, decimal Input, decimal CachedInput, decimal CacheWrite, decimal OutputPrice);
