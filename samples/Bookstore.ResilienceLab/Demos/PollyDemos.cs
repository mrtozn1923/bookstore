using System.Diagnostics;
using System.Net;
using System.Threading.RateLimiting;
using Polly;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Retry;
using Polly.Timeout;

namespace Bookstore.ResilienceLab.Demos;

public static class PollyDemos
{
    private static readonly Stopwatch Clock = Stopwatch.StartNew();

    private static void Log(string message) => Console.WriteLine($"[{Clock.Elapsed.TotalSeconds,5:0.00}s] {message}");

    // The first attempt hangs for 5 s, the second answers at once.
    private static Func<CancellationToken, ValueTask<string>> HangOnce()
    {
        var calls = 0;
        return async token =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), token);
            }

            return $"quote after {calls} call(s)";
        };
    }

    private static async Task RunAsync(string name, ResiliencePipeline pipeline)
    {
        var started = Clock.Elapsed;
        try
        {
            var result = await pipeline.ExecuteAsync(HangOnce());
            Log($"{name}: {result} in {(Clock.Elapsed - started).TotalSeconds:0.0} s");
        }
        catch (Exception ex)
        {
            Log($"{name}: {ex.GetType().Name} in {(Clock.Elapsed - started).TotalSeconds:0.0} s");
        }
    }

    // Strategy order and ShouldHandle decide whether a timeout is retried at all.
    public static async Task OrderAsync()
    {
        var retry = new RetryStrategyOptions { MaxRetryAttempts = 2, Delay = TimeSpan.FromMilliseconds(200) };

        await RunAsync("timeout outside retry", new ResiliencePipelineBuilder()
            .AddTimeout(TimeSpan.FromSeconds(2))
            .AddRetry(retry)
            .Build());

        await RunAsync("timeout inside retry ", new ResiliencePipelineBuilder()
            .AddRetry(retry)
            .AddTimeout(TimeSpan.FromSeconds(2))
            .Build());

        await RunAsync("inside, HttpRequestException only", new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromMilliseconds(200),
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
            })
            .AddTimeout(TimeSpan.FromSeconds(2))
            .Build());
    }

    // Polly's timeout is cooperative: code that ignores the token runs to the end.
    public static async Task OptimisticTimeoutAsync()
    {
        var pipeline = new ResiliencePipelineBuilder().AddTimeout(TimeSpan.FromSeconds(1)).Build();
        foreach (var honorsToken in new[] { true, false })
        {
            var started = Clock.Elapsed;
            try
            {
                await pipeline.ExecuteAsync(async token =>
                {
                    if (honorsToken)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(3), token);
                    }
                    else
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(3)); // a legacy SDK call without a CancellationToken
                    }
                });
                Log($"honors token={honorsToken}: completed in {(Clock.Elapsed - started).TotalSeconds:0.0} s");
            }
            catch (TimeoutRejectedException)
            {
                Log($"honors token={honorsToken}: TimeoutRejectedException in {(Clock.Elapsed - started).TotalSeconds:0.0} s");
            }
        }
    }

    // A generic retry that respects Retry-After and caps the wait with MaxDelay.
    public static async Task RetryAfterAsync()
    {
        using var server = new Flaky([429, 429], headers: new Dictionary<string, string> { ["Retry-After"] = "2" });
        using var client = new HttpClient { BaseAddress = server.Url };

        var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromMilliseconds(200),
                MaxDelay = TimeSpan.FromSeconds(5),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => r.StatusCode is HttpStatusCode.TooManyRequests or >= HttpStatusCode.InternalServerError),
                DelayGenerator = args =>
                {
                    // Retry-After: 2 (seconds) or an HTTP date; null means "use Delay/BackoffType".
                    var retryAfter = args.Outcome.Result?.Headers.RetryAfter;
                    var wait = retryAfter?.Delta ?? (retryAfter?.Date - DateTimeOffset.UtcNow);
                    return ValueTask.FromResult(wait);
                },
                OnRetry = args =>
                {
                    Log($"retry {args.AttemptNumber + 1} after {(int?)args.Outcome.Result?.StatusCode}, waiting {args.RetryDelay.TotalSeconds:0.00} s");
                    return default;
                }
            })
            .Build();

        using var response = await pipeline.ExecuteAsync(async token => await client.GetAsync("api/v2/quotes", token));
        Log($"final status {(int)response.StatusCode} after {server.Attempts} attempts");
    }

    // Closed -> Open -> HalfOpen -> Closed, then a manual isolate for a maintenance window.
    public static async Task CircuitBreakerAsync()
    {
        var manual = new CircuitBreakerManualControl();
        var state = new CircuitBreakerStateProvider();
        var pipeline = new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = 4,
                SamplingDuration = TimeSpan.FromSeconds(10),
                // Longer breaks when more calls failed: 4 failures -> 2 s, capped at 30 s.
                BreakDurationGenerator = args => ValueTask.FromResult(TimeSpan.FromSeconds(Math.Min(30, args.FailureCount * 0.5))),
                ManualControl = manual,
                StateProvider = state,
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(),
                OnOpened = args => { Log($"OnOpened   break={args.BreakDuration.TotalSeconds}s manual={args.IsManual}"); return default; },
                OnHalfOpened = _ => { Log("OnHalfOpened"); return default; },
                OnClosed = args => { Log($"OnClosed   manual={args.IsManual}"); return default; }
            })
            .Build();

        var carrierDown = true;
        async Task CallAsync(int i)
        {
            try
            {
                await pipeline.ExecuteAsync(_ => carrierDown
                    ? throw new HttpRequestException("Connection refused")
                    : ValueTask.CompletedTask);
                Log($"call {i}: ok (state {state.CircuitState})");
            }
            catch (Exception ex)
            {
                Log($"call {i}: {ex.GetType().Name} (state {state.CircuitState})");
            }
        }

        for (var i = 1; i <= 5; i++)
        {
            await CallAsync(i);
        }

        await Task.Delay(TimeSpan.FromSeconds(2.2));
        carrierDown = false;
        await CallAsync(6);
        await CallAsync(7);

        Log("maintenance window starts: IsolateAsync()");
        await manual.IsolateAsync();
        await CallAsync(8);
        await manual.CloseAsync();
        await CallAsync(9);
    }

    private static readonly ResiliencePropertyKey<int> OrderIdKey = new("bookstore.order-id");

    // ResilienceContext carries the caller's token and our own properties to every strategy.
    public static async Task ContextAsync()
    {
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 1,
                Delay = TimeSpan.Zero,
                OnRetry = args =>
                {
                    args.Context.Properties.TryGetValue(OrderIdKey, out var orderId);
                    Log($"OnRetry for order {orderId}, attempt {args.AttemptNumber + 1}");
                    return default;
                }
            })
            .Build();

        using var cts = new CancellationTokenSource();
        var context = ResilienceContextPool.Shared.Get("carrier-quote", cts.Token);
        context.Properties.Set(OrderIdKey, 1001);
        try
        {
            var attempts = 0;
            await pipeline.ExecuteAsync(ctx =>
            {
                Log($"attempt {++attempts}: operation key={ctx.OperationKey}, same token={ctx.CancellationToken == cts.Token}");
                return attempts == 1 ? throw new HttpRequestException("Connection reset") : ValueTask.CompletedTask;
            }, context);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }

    // A concurrency limiter is a bulkhead: at most 2 calls in flight, no queue.
    public static async Task BulkheadAsync()
    {
        var pipeline = new ResiliencePipelineBuilder()
            .AddConcurrencyLimiter(permitLimit: 2, queueLimit: 0)
            .Build();

        var calls = Enumerable.Range(1, 5).Select(async i =>
        {
            try
            {
                await pipeline.ExecuteAsync(async token => await Task.Delay(500, token));
                return $"call {i}: ok";
            }
            catch (RateLimiterRejectedException)
            {
                return $"call {i}: RateLimiterRejectedException";
            }
        });

        foreach (var line in await Task.WhenAll(calls))
        {
            Console.WriteLine(line);
        }
    }
}
