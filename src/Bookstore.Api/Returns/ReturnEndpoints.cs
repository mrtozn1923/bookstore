using System.Security.Claims;
using Bookstore.Api.Ordering;

namespace Bookstore.Api.Returns;

public static class ReturnEndpoints
{
    public sealed record ReturnText(string Text);

    public sealed record ReturnCheck(ReturnRequestDraft? Draft, IReadOnlyList<string> Problems);

    public static IEndpointRouteBuilder MapReturnEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/returns/extract", async Task<IResult> (
            ReturnText body, ClaimsPrincipal user, ReturnRequestExtractor extractor, ReturnPolicy policy, CancellationToken ct) =>
        {
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out Guid customerId))
            {
                return TypedResults.Unauthorized();
            }

            ExtractionResult result = await extractor.ExtractAsync(body.Text, ct);
            if (result.Draft is null)
            {
                return TypedResults.Ok(new ReturnCheck(null, [result.Problem ?? "No draft."]));
            }

            // Nothing is created here: the draft and its problems go to the customer or a support agent to confirm.
            return TypedResults.Ok(new ReturnCheck(result.Draft, policy.Check(result.Draft, new CurrentCustomer(customerId))));
        });

        return app;
    }
}
