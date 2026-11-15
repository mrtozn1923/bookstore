using Anthropic;
using Anthropic.Models.Models;

namespace Bookstore.Api.Ai;

public static class ModelEndpoints
{
    public sealed record ModelRow(string Id, string DisplayName, long? MaxInputTokens, long? MaxTokens, string Lifecycle);

    // Development only: lists the models this key can use.
    public static IEndpointRouteBuilder MapModelEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/ai/models", async (IAnthropicClient claude, CancellationToken ct) =>
        {
            List<ModelRow> rows = [];
            ModelListPage page = await claude.Models.List(cancellationToken: ct);
            await foreach (ModelInfo model in page.Paginate(ct))
            {
                rows.Add(new ModelRow(model.ID, model.DisplayName, model.MaxInputTokens, model.MaxTokens, model.Lifecycle.Raw()));
            }

            return TypedResults.Ok(rows);
        });

        return app;
    }
}
