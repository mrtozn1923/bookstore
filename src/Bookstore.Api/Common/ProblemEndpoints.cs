using System.Net;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Bookstore.Api.Common;

public sealed record ProblemTypeResponse(string Type, string Code, int Status, string Title);

// The type URIs point here: a machine-readable catalog and one human-readable page per type.
public static class ProblemEndpoints
{
    public static IEndpointRouteBuilder MapProblemEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/problems", () => TypedResults.Ok(ProblemCatalog.All
            .Select(p => new ProblemTypeResponse(p.Type, p.Code, p.Status, ProblemCatalog.Title(p)))))
            .WithName("listProblemTypes");
        app.MapGet("/problems/{code}", Results<ContentHttpResult, ProblemHttpResult> (string code) =>
            ProblemCatalog.All.FirstOrDefault(p => p.Code == code) is { } type
                ? TypedResults.Content(
                    $"<!doctype html><title>{type.Code}</title><h1>{WebUtility.HtmlEncode(ProblemCatalog.Title(type))}</h1>" +
                    $"<p>HTTP {type.Status}. Type URI: <code>{type.Type}</code></p>", "text/html")
                : ProblemCatalog.Create(ProblemCatalog.NotFound, [$"/problems/{code}"]))
            .WithName("getProblemType");
        return app;
    }
}
