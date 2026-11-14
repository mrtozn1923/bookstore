using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Bookstore.Api.Filters;

public class ExecutionTimeFilter : IAsyncActionFilter
{
    private readonly ILogger<ExecutionTimeFilter> _logger;

    public ExecutionTimeFilter(ILogger<ExecutionTimeFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        // calling next() runs the action (and the filters after this one)
        ActionExecutedContext executed = await next();

        stopwatch.Stop();
        _logger.LogInformation("{Action} took {Elapsed} ms (exception: {HasException})",
            context.ActionDescriptor.DisplayName, stopwatch.ElapsedMilliseconds, executed.Exception is not null);
    }
}
