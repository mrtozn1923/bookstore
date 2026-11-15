# Bookstore filter language

Code for the "Compilers and Programming Languages" series on mertozen.com.

```bash
dotnet build
dotnet test --project tests/Bookstore.Filter.Tests
dotnet run --project src/Bookstore.Filter.Cli -- tokens 'price <= 300 and author = "Jane Austen"'
dotnet run --project src/Bookstore.Filter.Cli -- ast 'price < 300 and author = "Jane Austen"'
dotnet run --project src/Bookstore.Filter.Cli -- run 'price < 300 and author = "Jane Austen"'
dotnet run --project src/Bookstore.Filter.Cli -- nest 100000 pratt
dotnet run --project src/Bookstore.Filter.Cli -- check 'price < 300 and author = "Jane Austen"'
dotnet run --project src/Bookstore.Filter.Cli -- sql 'price < 300 and author = "Jane Austen"'
dotnet run --project src/Bookstore.Filter.Cli -- chain 5000

# samples
dotnet run -c Release --project samples/PipelineTour
dotnet run --project samples/RoslynTrivia
dotnet run --project samples/RegexLexer
dotnet run --project samples/RoslynSemantic
cd samples/ts-erasure && npm install && npx tsc && node dist/book.js
node samples/v8-tiers/hot.js

# 05: IL, tiers, PGO, escape analysis (net9.0 needs the .NET 9 runtime)
DOTNET_JitDisasmSummary=1 dotnet run -c Release --project samples/JitLab -f net10.0 -- tiers
DOTNET_JitDisasm=CountMatches dotnet run -c Release --project samples/JitLab -f net10.0 -- pgo
dotnet run -c Release --project samples/JitLab -f net9.0 -- alloc
dotnet run -c Release --project samples/JitLab -f net10.0 -- alloc
dotnet run -c Release --project samples/JitLab -f net10.0 -- warmup
dotnet run -c Release --project benchmarks/Bookstore.Filter.Benchmarks -- --filter '*RuleBenchmarks*'
node --allow-natives-syntax samples/v8-tiers/shapes.js
node --trace-deopt samples/v8-tiers/deopt.js

# 06: JIT vs ReadyToRun vs Native AOT (Native AOT needs the platform toolchain, e.g. the VS C++ workload on Windows)
dotnet publish samples/Bookstore.Api -c Release -o publish/jit
dotnet publish samples/Bookstore.Api -c Release -p:PublishReadyToRun=true -o publish/r2r
dotnet publish samples/Bookstore.Api -c Release -p:PublishAot=true -o publish/aot
dotnet run -c Release --project samples/StartupProbe -- 5 publish/aot/Bookstore.Api
dotnet run -c Release --project benchmarks/Bookstore.Filter.Benchmarks -- --filter '*CompileBenchmarks*'

# 07: garbage collection
dotnet run -c Release --project samples/GcLab -- gens
dotnet run -c Release --project samples/GcLab -- memory
dotnet run -c Release --project samples/GcLab -- filter interpreter 5
DOTNET_gcServer=1 dotnet run -c Release --project samples/GcLab -- modes 8
dotnet run -c Release --project benchmarks/Bookstore.Filter.Benchmarks -- --filter '*EvaluationBenchmarks*'
```
