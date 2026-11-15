using Anthropic.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace Bookstore.Api.Ai;

// Turns SDK exceptions that escaped a service into ProblemDetails; the SDK has already retried by then.
public sealed class ClaudeExceptionHandler(IProblemDetailsService problemDetails, ILogger<ClaudeExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not AnthropicException)
        {
            return false;
        }

        ClaudeFailure failure = ClaudeFailureClassifier.Classify(exception);
        // The request id is what Anthropic support asks for; log it with every failure.
        logger.LogError(exception, "Claude call failed: kind={Kind} status={Status} requestId={RequestId} errorCode={ErrorCode}",
            failure.Kind, failure.StatusCode, failure.RequestId, failure.ErrorCode);

        context.Response.StatusCode = failure.RetryLater ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status502BadGateway;
        if (failure.RetryLater)
        {
            context.Response.Headers.RetryAfter = "30";
        }

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails =
            {
                Title = failure.RetryLater ? "The assistant is busy. Try again shortly." : "The assistant is not available.",
                Extensions = { ["kind"] = failure.Kind.ToString() },
            },
        });
    }
}
