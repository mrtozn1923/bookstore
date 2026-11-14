using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Bookstore.Api.Exceptions;

namespace Bookstore.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // the client gave up; this is not an error
            _logger.LogInformation("Request {Path} was cancelled by the client", context.Request.Path);
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 499;
            }
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            await WriteProblemAsync(context, exception);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        (int statusCode, string title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogWarning("{ExceptionType}: {Message}", exception.GetType().Name, exception.Message);
        }

        // we wrote the domain messages, so they are safe; an unexpected error's message only in Development
        bool showMessage = exception is DomainException || _environment.IsDevelopment();

        ProblemDetails problem = new()
        {
            Status = statusCode,
            Title = title,
            Detail = showMessage ? exception.Message : null,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
    }
}
