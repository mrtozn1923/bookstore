namespace Bookstore.Api;

public class ValidateBookFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // The handler's first argument is CreateBookRequest
        var request = context.GetArgument<CreateBookRequest>(0);

        if (string.IsNullOrWhiteSpace(request.Title) || request.Price <= 0)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Book"] = new[] { "Title is required and Price must be greater than zero." }
            });
        }

        // All good: move on to the next filter or the handler
        return await next(context);
    }
}
