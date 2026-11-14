using Bookstore.Search;
using Elastic.Clients.Elasticsearch;

namespace Bookstore.Api;

public static class BookEndpoints
{
    public static IEndpointRouteBuilder MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/books/{isbn}", async (string isbn, ElasticsearchClient client, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
        {
            GetResponse<Book> response = await client.GetAsync<Book>(isbn, cancellationToken);

            if (response.IsValidResponse)
            {
                return response.Found ? Results.Ok(response.Source) : Results.NotFound();
            }

            // Transport errors and server errors both end up here; log the reason, not the request headers.
            ILogger logger = loggerFactory.CreateLogger("Bookstore.Api.BookEndpoints");
            logger.LogError("Elasticsearch get failed: HTTP {StatusCode} {ErrorType} {Reason}",
                response.ApiCallDetails.HttpStatusCode,
                response.ElasticsearchServerError?.Error?.Type,
                response.ElasticsearchServerError?.Error?.Reason ?? response.ApiCallDetails.OriginalException?.Message);
            return Results.Problem("Search is unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable);
        });

        return app;
    }
}
