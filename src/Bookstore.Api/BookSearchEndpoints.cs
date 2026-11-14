using Bookstore.Search;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace Bookstore.Api;

public sealed record BookSearchHit(string Isbn, string Title, IReadOnlyList<string> Authors, string Language, decimal Price, double? Score);

public sealed record BookSearchResult(long Total, int Page, int PageSize, IReadOnlyList<BookSearchHit> Items);

public static class BookSearchEndpoints
{
    private const int MaxPageSize = 50;
    private const int MaxResultWindow = 10_000;

    // Analyzed variants of title and description (post 04); boosts are still guesses until post 07.
    private static readonly string[] TextFields =
    [
        "title^3", "title.tr^2", "title.folded^2", "title.en",
        "description", "description.tr", "description.folded", "description.en"
    ];

    public static IEndpointRouteBuilder MapBookSearchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/books/search", async (string? q, string? category, string? language, decimal? minPrice, decimal? maxPrice,
            int? page, int? pageSize, ElasticsearchClient client, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
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

            SearchResponse<Book> response = await client.SearchAsync<Book>(s => s
                .From(from)
                .Size(size)
                .TrackTotalHits(new TrackHits(true))
                .Query(BuildQuery(q, category, language, minPrice, maxPrice)), cancellationToken);

            if (!response.IsValidResponse)
            {
                ILogger logger = loggerFactory.CreateLogger("Bookstore.Api.BookSearchEndpoints");
                logger.LogError("Elasticsearch search failed: HTTP {StatusCode} {ErrorType} {Reason}",
                    response.ApiCallDetails.HttpStatusCode,
                    response.ElasticsearchServerError?.Error?.Type,
                    response.ElasticsearchServerError?.Error?.Reason ?? response.ApiCallDetails.OriginalException?.Message);
                return Results.Problem("Search is unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            List<BookSearchHit> items = response.Hits
                .Select(h => new BookSearchHit(h.Source!.Isbn, h.Source.Title,
                    h.Source.Authors.Where(a => a.Role == "author").Select(a => a.Name).ToList(),
                    h.Source.Language, h.Source.Price, h.Score))
                .ToList();
            return Results.Ok(new BookSearchResult(response.Total, currentPage, size, items));
        });

        return app;
    }

    public static Query BuildQuery(string? text, string? category, string? language, decimal? minPrice, decimal? maxPrice)
    {
        // Query context: what the user typed decides the score.
        Query textQuery = string.IsNullOrWhiteSpace(text)
            ? new MatchAllQuery()
            : new BoolQuery
            {
                Should =
                [
                    new MultiMatchQuery { Query = text, Fields = Fields.FromStrings(TextFields) },
                    // Authors are nested (post 03): a root-level multi_match cannot see them.
                    new NestedQuery
                    {
                        Path = "authors",
                        ScoreMode = ChildScoreMode.Max,
                        Query = new MatchQuery { Field = "authors.name", Query = text }
                    }
                ],
                MinimumShouldMatch = 1
            };

        // Filter context: yes/no conditions, no score, cacheable.
        List<Query> filters = [];
        if (!string.IsNullOrWhiteSpace(category))
        {
            filters.Add(new TermQuery { Field = "categories", Value = category });
        }
        if (!string.IsNullOrWhiteSpace(language))
        {
            filters.Add(new TermQuery { Field = "language", Value = language });
        }
        if (minPrice is not null || maxPrice is not null)
        {
            filters.Add(new NumberRangeQuery
            {
                Field = "price",
                Gte = minPrice is null ? null : (double)minPrice.Value,
                Lte = maxPrice is null ? null : (double)maxPrice.Value
            });
        }

        return new BoolQuery { Must = [textQuery], Filter = filters };
    }
}
