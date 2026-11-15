namespace Bookstore.Api.Catalog.Publishers;

public static class PublisherCatalogEndpoints
{
    public sealed record CatalogQuestion(string Question);

    public static IEndpointRouteBuilder MapPublisherCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder publishers = app.MapGroup("/api/publishers/{publisherId}/catalog");

        publishers.MapPost("/", async Task<IResult> (
            string publisherId, IFormFile catalog, PublisherCatalogService service, CancellationToken ct) =>
        {
            if (catalog.ContentType != "application/pdf")
            {
                return TypedResults.BadRequest("Only PDF catalogs are accepted.");
            }

            await using Stream pdf = catalog.OpenReadStream();
            PublisherCatalogFile file = await service.UploadAsync(publisherId, pdf, catalog.FileName, ct);
            // The file_id is not returned to the caller; it stays a server-side reference.
            return TypedResults.Ok(new { file.FileName, file.ExpiresAt });
        }).DisableAntiforgery();

        publishers.MapPost("/questions", async Task<IResult> (
            string publisherId, CatalogQuestion body, PublisherCatalogService service, CancellationToken ct) =>
        {
            CatalogAnswer? answer = await service.AskAsync(publisherId, body.Question, ct);
            return answer is null
                ? TypedResults.NotFound("No current catalog for this publisher. Upload it again.")
                : TypedResults.Ok(answer);
        });

        return app;
    }
}
