using System.Globalization;
using Bookstore.ResilienceLab.Demos;

// Same number and date format on every machine.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

// Usage: dotnet run --project samples/Bookstore.ResilienceLab -- <demo>
var demos = new Dictionary<string, Func<Task>>
{
    ["clients"] = ClientsDemo.RunAsync,
    ["lifetime"] = FactoryDemos.LifetimeAsync,
    ["captive"] = FactoryDemos.CaptiveAsync,
    ["scope"] = FactoryDemos.ScopeAsync,
    ["logging"] = FactoryDemos.LoggingAsync,
    ["timeout"] = FactoryDemos.TimeoutAsync,
    ["singleton-handler"] = FactoryDemos.SingletonHandlerAsync,
    ["base-address"] = FactoryDemos.BaseAddressAsync,
    ["stream"] = FactoryDemos.StreamAsync,
    ["standard-defaults"] = StandardPipelineDemos.DefaultsAsync,
    ["standard-retry"] = StandardPipelineDemos.RetryAsync,
    ["standard-post"] = StandardPipelineDemos.PostAsync,
    ["standard-validation"] = StandardPipelineDemos.ValidationAsync,
    ["client-timeout"] = StandardPipelineDemos.ClientTimeoutAsync,
    ["stacked"] = StandardPipelineDemos.StackedAsync,
    ["hedging"] = StandardPipelineDemos.HedgingAsync,
    ["order"] = PollyDemos.OrderAsync,
    ["optimistic-timeout"] = PollyDemos.OptimisticTimeoutAsync,
    ["retry-after"] = PollyDemos.RetryAfterAsync,
    ["circuit-breaker"] = PollyDemos.CircuitBreakerAsync,
    ["context"] = PollyDemos.ContextAsync,
    ["bulkhead"] = PollyDemos.BulkheadAsync,
    ["migration-v7"] = MigrationDemos.PollyV7Async,
    ["migration-v8"] = MigrationDemos.PollyV8Async,
    ["migration-bridge"] = MigrationDemos.BridgeAsync
};

if (args.Length == 0 || !demos.TryGetValue(args[0], out var demo))
{
    Console.WriteLine($"Demos: {string.Join(", ", demos.Keys)}");
    return;
}

await demo();
