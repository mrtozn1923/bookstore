using BenchmarkDotNet.Running;
using Bookstore.Benchmarks;

// dotnet run -c Release -f net10.0 -- --filter "*Pricing*"
BenchmarkSwitcher.FromAssembly(typeof(PricingBenchmarks).Assembly).Run(args);
