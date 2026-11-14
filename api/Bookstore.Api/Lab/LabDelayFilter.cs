namespace Bookstore.Api.Lab;

// Lab only: simulates a slow backend (database, other services) behind /api/books. 0 = off.
public sealed class LabDelayFilter(IConfiguration configuration) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        int delayMs = configuration.GetValue("Lab:ApiDelayMs", 0);
        if (delayMs > 0)
        {
            await Task.Delay(delayMs, context.HttpContext.RequestAborted);
        }
        return await next(context);
    }
}
