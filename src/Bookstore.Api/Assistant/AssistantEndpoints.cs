using System.Security.Claims;
using Bookstore.Api.Ordering;

namespace Bookstore.Api.Assistant;

public static class AssistantEndpoints
{
    public sealed record AssistantQuestion(string Question);

    public static IEndpointRouteBuilder MapAssistantEndpoints(this IEndpointRouteBuilder app)
    {
        // The request's CancellationToken fires when the browser disconnects; the Claude stream stops with it.
        app.MapPost("/api/assistant/stream", (AssistantQuestion body, ShoppingAssistant assistant, CancellationToken ct) =>
            TypedResults.ServerSentEvents(assistant.StreamAsync(body.Question, ct)));

        // The customer comes from the authenticated user, never from the request body or the model.
        app.MapPost("/api/assistant/ask", async Task<IResult> (
            AssistantQuestion body, ClaimsPrincipal user, AssistantAgent agent, CancellationToken ct) =>
        {
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out Guid customerId))
            {
                return TypedResults.Unauthorized();
            }

            return TypedResults.Ok(await agent.AskAsync(body.Question, new CurrentCustomer(customerId), ct));
        });

        return app;
    }
}
