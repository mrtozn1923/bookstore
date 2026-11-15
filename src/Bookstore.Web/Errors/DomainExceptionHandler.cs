using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Bookstore.Web.Errors;

public sealed class DomainExceptionHandler(
    IProblemDetailsService problemDetails,
    ITempDataDictionaryFactory tempDataFactory,
    ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not DomainException domain)
        {
            return false; // not ours: the 500 page takes over
        }

        // Request.Path is already the error path (/error) here; the original path is in the feature
        string path = context.Features.Get<IExceptionHandlerPathFeature>()?.Path ?? context.Request.Path;
        logger.LogInformation("Rule {Rule} stopped {Method} {Path}: {Message}",
            domain.GetType().Name, context.Request.Method, path, domain.UserMessage);

        if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = domain.StatusCode;
            return await problemDetails.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                Exception = exception,
                ProblemDetails = { Status = domain.StatusCode, Title = domain.UserMessage }
            });
        }

        if (HttpMethods.IsPost(context.Request.Method))
        {
            // Form post: back to the page the form came from, with a flash message (Post-Redirect-Get)
            ITempDataDictionary tempData = tempDataFactory.GetTempData(context);
            tempData["Flash"] = domain.UserMessage;
            tempData.Save();
            context.Response.Redirect(LocalReferer(context) ?? "/");
            return true;
        }

        return false; // GET: the error page shows UserMessage with the status from StatusCodeSelector
    }

    private static string? LocalReferer(HttpContext context)
    {
        if (!Uri.TryCreate(context.Request.Headers.Referer.ToString(), UriKind.Absolute, out Uri? referer)
            || !string.Equals(referer.Authority, context.Request.Host.Value, StringComparison.OrdinalIgnoreCase))
        {
            return null; // never redirect to another site
        }

        return referer.PathAndQuery;
    }
}
