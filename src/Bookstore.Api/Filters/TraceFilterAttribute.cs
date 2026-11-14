using Microsoft.AspNetCore.Mvc.Filters;

namespace Bookstore.Api.Filters;

public class TraceFilterAttribute : ActionFilterAttribute
{
    private readonly string _name;

    public TraceFilterAttribute(string name)
    {
        _name = name;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        GetLogger(context.HttpContext).LogInformation("{Name} OnActionExecuting", _name);
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        GetLogger(context.HttpContext).LogInformation("{Name} OnActionExecuted", _name);
    }

    // attributes can't take constructor injection; we pull the logger from the request
    private static ILogger GetLogger(HttpContext httpContext) =>
        httpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Trace");
}
