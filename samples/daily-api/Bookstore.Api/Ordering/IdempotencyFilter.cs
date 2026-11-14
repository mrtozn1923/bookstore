using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Bookstore.Api.Ordering;

[AttributeUsage(AttributeTargets.Method)]
public sealed class IdempotentAttribute : Attribute;

public static class IdempotencyFilter
{
    public const string HeaderName = "Idempotency-Key";
    private static readonly ConcurrentDictionary<string, OrderDto> Seen = new();

    // Factory uygulama açılırken her endpoint için bir kez çalışır
    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext factoryContext, EndpointFilterDelegate next)
    {
        // [Idempotent] taşımayan handler'lar için filter hiç devreye girmez
        if (factoryContext.MethodInfo.GetCustomAttribute<IdempotentAttribute>() is null)
        {
            return next;
        }

        return async context =>
        {
            string? key = context.HttpContext.Request.Headers[HeaderName];
            if (string.IsNullOrWhiteSpace(key))
            {
                return TypedResults.Problem(
                    title: $"The {HeaderName} header is required.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            if (Seen.TryGetValue(key, out OrderDto? existing))
            {
                // Aynı istek tekrar geldi: handler'a gitmeden önceki sonucu dön
                context.HttpContext.Response.Headers["Idempotent-Replayed"] = "true";
                return TypedResults.Ok(existing);
            }

            object? result = await next(context);
            if (result is Results<Created<OrderDto>, ValidationProblem> { Result: Created<OrderDto> { Value: { } order } })
            {
                Seen[key] = order;
            }
            return result;
        };
    }
}
