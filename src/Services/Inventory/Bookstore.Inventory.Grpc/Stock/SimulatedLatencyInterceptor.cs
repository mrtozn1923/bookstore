using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Bookstore.Inventory.Stock;

// Development aid for resilience experiments: registered only when Inventory:SimulatedLatency is set.
public sealed class SimulatedLatencyInterceptor(IConfiguration configuration, ILogger<SimulatedLatencyInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        TimeSpan latency = configuration.GetValue<TimeSpan>("Inventory:SimulatedLatency");
        logger.LogInformation("Simulating {Latency} latency; caller deadline is {Deadline:O}", latency, context.Deadline);
        await Task.Delay(latency, context.CancellationToken);
        return await continuation(request, context);
    }
}
