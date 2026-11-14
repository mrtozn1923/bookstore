using Bookstore.Search;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;

namespace Bookstore.Api;

// Highlights are HTML: the text is escaped by Elasticsearch (encoder html), only <mark> tags are added.
public sealed record BookSearchHit(string Isbn, string Title, IReadOnlyList<string> Authors, string Language, decimal Price, double? Score,
    string? TitleHighlight, string? DescriptionHighlight);

public sealed record BookSearchResult(long Total, int Page, int PageSize, IReadOnlyList<BookSearchHit> Items, string? DidYouMean,
    IReadOnlyList<Facet> Facets);

public static class BookSearchEndpoints
{
    private const int MaxPageSize = BookSearchRequestBuilder.MaxPageSize;
    private const int MaxResultWindow = BookSearchRequestBuilder.MaxResultWindow;

    public static IEndpointRouteBuilder MapBookSearchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/books/search", async (string? q, string? category, string? language, decimal? minPrice, decimal? maxPrice,
            decimal? minRating, string? sort, int? page, int? pageSize, ElasticsearchClient client, ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            int currentPage = page ?? 1;
            int size = pageSize ?? 20;
            if (currentPage < 1 || size < 1 || size > MaxPageSize)
            {
                return Results.Problem($"page must be >= 1 and pageSize between 1 and {MaxPageSize}.", statusCode: StatusCodes.Status400BadRequest);
            }

            int from = (currentPage - 1) * size;
            if (from + size > MaxResultWindow)
            {
                return Results.Problem("Only the first 10,000 results can be paged. Narrow the search with filters.", statusCode: StatusCodes.Status400BadRequest);
            }

            BookSearchInput input = new(new BookSearchFilter(q, category, language, minPrice, maxPrice, minRating), sort, currentPage, size);
            SearchRequest<Book> request = BookSearchRequestBuilder.Build(input, SearchTuning.Tuned);

            SearchResponse<Book> response = await client.SearchAsync<Book>(request, cancellationToken);
            if (!response.IsValidResponse)
            {
                ILogger logger = loggerFactory.CreateLogger("Bookstore.Api.BookSearchEndpoints");
                logger.LogError("Elasticsearch search failed: HTTP {StatusCode} {ErrorType} {Reason}",
                    response.ApiCallDetails.HttpStatusCode,
                    response.ElasticsearchServerError?.Error?.Type,
                    response.ElasticsearchServerError?.Error?.Reason ?? response.ApiCallDetails.OriginalException?.Message);
                return Results.Problem("Search is unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            List<BookSearchHit> items = response.Hits.Select(ToHit).ToList();
            return Results.Ok(new BookSearchResult(response.Total, currentPage, size, items, BookSearchRequestBuilder.ReadDidYouMean(response, q),
                BookFacets.Read(response.Aggregations)));
        });

        return app;
    }

    private static BookSearchHit ToHit(Hit<Book> hit)
    {
        Book book = hit.Source!;
        string? title = hit.Highlight?.GetValueOrDefault("title")?.FirstOrDefault();
        string? description = hit.Highlight?.GetValueOrDefault("description")?.FirstOrDefault();
        return new BookSearchHit(book.Isbn, book.Title,
            book.Authors.Where(a => a.Role == "author").Select(a => a.Name).ToList(),
            book.Language, book.Price, hit.Score, title, description);
    }
}
