using Bookstore.Institutional.Application;
using Bookstore.Institutional.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api;

// State machine'de olmayan geçiş -> 409, iş kuralı ihlali -> 422, kayıt yok -> 404
public sealed class OrderExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken ct)
    {
        (int status, string title) = exception switch
        {
            InvalidOrderTransitionException => (StatusCodes.Status409Conflict, "Invalid status transition"),
            OrderRuleException e => (StatusCodes.Status422UnprocessableEntity, $"Rule violated: {e.Rule}"),
            OrderNotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            _ => (0, "")
        };
        if (status == 0) return false;

        http.Response.StatusCode = status;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = exception.Message },
            Exception = exception
        });
    }
}
