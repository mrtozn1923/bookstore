using Bookstore.Invoicing;
using Bookstore.Ordering;
using Bookstore.Payments;
using Bookstore.Shipping;
using Microsoft.AspNetCore.Diagnostics;

namespace Bookstore.Infrastructure;

// Turns our own integration exceptions into ProblemDetails; provider details never reach the client.
public sealed class ExternalServiceExceptionHandler(IProblemDetailsService problemDetails, ILogger<ExternalServiceExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        (int status, string title)? mapped = exception switch
        {
            OrderStateException =>
                (StatusCodes.Status409Conflict, exception.Message),
            PaymentAttemptConflictException =>
                (StatusCodes.Status409Conflict, "A previous payment for this order is still being verified."),
            PaymentGatewayException { Kind: PaymentFailureKind.UnknownOutcome } =>
                (StatusCodes.Status202Accepted, "Payment is being verified."),
            PaymentGatewayException { Kind: PaymentFailureKind.Transient or PaymentFailureKind.Unavailable } =>
                (StatusCodes.Status503ServiceUnavailable, "Payment service is temporarily unavailable."),
            ShippingCarrierException { Kind: ShippingFailureKind.Transient or ShippingFailureKind.Unavailable } =>
                (StatusCodes.Status503ServiceUnavailable, "Shipping service is temporarily unavailable."),
            InvoiceIntegratorException { Kind: InvoiceFailureKind.Rejected } =>
                (StatusCodes.Status422UnprocessableEntity, "The e-invoice was rejected by the integrator."),
            InvoiceIntegratorException { Kind: InvoiceFailureKind.Transient or InvoiceFailureKind.Unavailable } =>
                (StatusCodes.Status503ServiceUnavailable, "E-invoice service is temporarily unavailable."),
            PaymentGatewayException or ShippingCarrierException or InvoiceIntegratorException =>
                (StatusCodes.Status502BadGateway, "An external service returned an unexpected response."),
            _ => null
        };

        if (mapped is null)
        {
            return false;
        }

        logger.LogWarning(exception, "External service failure mapped to {Status}", mapped.Value.status);
        context.Response.StatusCode = mapped.Value.status;
        if (exception is PaymentGatewayException { RetryAfter: { } retryAfter })
        {
            context.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
        }

        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = mapped.Value.status, Title = mapped.Value.title };
        // The integrator's code helps support; its message and the SOAP details stay in our logs.
        if (exception is InvoiceIntegratorException { Code: { } code })
        {
            problem.Extensions["integratorCode"] = code;
        }

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = problem
        });
    }
}
