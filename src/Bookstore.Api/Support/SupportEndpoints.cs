namespace Bookstore.Api.Support;

public static class SupportEndpoints
{
    public sealed record CustomerMessage(string Text, string? OperatorNote);

    public static IEndpointRouteBuilder MapSupportEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder support = app.MapGroup("/api/support");

        support.MapPost("/conversations/{conversationId:guid}/messages", async (
            Guid conversationId, CustomerMessage body, SupportChatService chat, CancellationToken ct) =>
            TypedResults.Ok(await chat.SendAsync(conversationId, body.Text, body.OperatorNote, ct)));

        support.MapPost("/invoices/analyze", async Task<IResult> (
            IFormFile invoice, string question, InvoiceAnalyzer analyzer, CancellationToken ct) =>
        {
            if (invoice.ContentType != "application/pdf")
            {
                return TypedResults.BadRequest("Only PDF invoices are accepted.");
            }

            using MemoryStream buffer = new();
            await invoice.CopyToAsync(buffer, ct);
            InvoiceAnswer? answer = await analyzer.AnalyzeAsync(buffer.ToArray(), question, ct);
            return answer is null
                ? TypedResults.Problem("The invoice is too large to analyze.", statusCode: StatusCodes.Status413PayloadTooLarge)
                : TypedResults.Ok(answer);
        }).DisableAntiforgery();

        return app;
    }
}
