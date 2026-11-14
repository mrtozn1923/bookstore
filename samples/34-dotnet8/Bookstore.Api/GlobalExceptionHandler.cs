using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Leave exceptions we don't recognize to the next handler
        if (exception is not BookNotFoundException)
        {
            return false;
        }

        logger.LogWarning("Handled: {Message}", exception.Message);

        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Book not found",
            Detail = exception.Message
        }, options: null, contentType: "application/problem+json", cancellationToken);

        return true;
    }
}
