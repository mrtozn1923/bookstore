using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Bookstore.Api.Ai;
using Bookstore.Api.Returns;

// Usage:
//   dotnet run -- estimate [inputTokens] [outputTokens]   price math only, no API call
//   dotnet run -- run                                     needs ANTHROPIC_API_KEY, calls the API
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
string mode = args.Length > 0 ? args[0] : "estimate";

if (mode == "estimate")
{
    long input = args.Length > 1 ? long.Parse(args[1]) : 450;
    long output = args.Length > 2 ? long.Parse(args[2]) : 60;
    TokenUsage perRequest = new(input, output, CacheRead: 0, CacheWrite: 0, Thinking: 0);

    Console.WriteLine($"Assumed tokens per request: input={input} output={output}");
    Console.WriteLine($"{"model",-20}{"per request",14}{"per 1,000",12}");
    foreach (string model in ClaudePricing.KnownModels)
    {
        decimal one = ClaudePricing.EstimateUsd(model, perRequest);
        Console.WriteLine($"{model,-20}{one,14:0.000000}{one * 1000,12:0.0000}");
    }

    return;
}

List<EvalCase> cases = JsonSerializer.Deserialize<List<EvalCase>>(
    File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "returns-eval.json")),
    new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

// Each row is one configuration; Haiku 5.5 also runs once with thinking turned off.
(string Model, Effort Effort, ThinkingConfigParam? Thinking)[] grid =
[
    ("claude-haiku-5-5", Effort.Low, new ThinkingConfigDisabled()),
    ("claude-haiku-5-5", Effort.Low, null),
    ("claude-haiku-5-5", Effort.Medium, null),
    ("claude-sonnet-5-5", Effort.Low, null),
    ("claude-sonnet-5-5", Effort.Medium, null),
    ("claude-sonnet-5-5", Effort.High, null),
    ("claude-opus-5-5", Effort.Low, null),
    ("claude-opus-5-5", Effort.Medium, null),
    ("claude-opus-5-5", Effort.High, null),
];

using AnthropicClient client = new();
ReturnClassifier classifier = new(client);

Console.WriteLine($"{"model",-20}{"effort",-8}{"thinking",-10}{"accuracy",10}{"p50 ms",9}{"out tok",9}{"USD",10}");
foreach ((string model, Effort effort, ThinkingConfigParam? thinking) in grid)
{
    int correct = 0;
    long outputTokens = 0;
    decimal cost = 0;
    List<long> latencies = [];

    // Sequential on purpose: a small eval should not hit the rate limit.
    foreach (EvalCase item in cases)
    {
        Stopwatch watch = Stopwatch.StartNew();
        Classification result = await classifier.ClassifyAsync(item.Text, new ClassifierSettings(model, effort, thinking), CancellationToken.None);
        latencies.Add(watch.ElapsedMilliseconds);

        correct += result.Reason?.ToString() == item.Expected ? 1 : 0;
        outputTokens += result.Usage.Output;
        cost += ClaudePricing.EstimateUsd(model, result.Usage);
    }

    latencies.Sort();
    Console.WriteLine($"{model,-20}{effort.ToString().ToLowerInvariant(),-8}{(thinking is null ? "default" : "off"),-10}{(double)correct / cases.Count,10:P0}{latencies[latencies.Count / 2],9}{outputTokens,9}{cost,10:0.0000}");
}

internal sealed record EvalCase(string Text, string Expected);
