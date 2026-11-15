namespace Bookstore.Api.Assistant;

public static class AssistantEndpoints
{
    public sealed record AssistantQuestion(string Question);

    public static IEndpointRouteBuilder MapAssistantEndpoints(this IEndpointRouteBuilder app)
    {
        // The request's CancellationToken fires when the browser disconnects; the Claude stream stops with it.
        app.MapPost("/api/assistant/stream", (AssistantQuestion body, ShoppingAssistant assistant, CancellationToken ct) =>
            TypedResults.ServerSentEvents(assistant.StreamAsync(body.Question, ct)));

        return app;
    }
}
