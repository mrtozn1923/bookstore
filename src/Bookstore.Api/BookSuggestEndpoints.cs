using Bookstore.Search;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace Bookstore.Api;

public sealed record BookSuggestion(string Isbn, string Title);

public static class BookSuggestEndpoints
{
    public const string CachePolicy = "suggest";
    private const int MaxSuggestions = 8;

    public static IEndpointRouteBuilder MapBookSuggestEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/books/suggest", async (string? q, string? category, ElasticsearchClient client, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
        {
            // One or two letters match thousands of titles; start suggesting from the second character.
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            {
                return Results.Ok(Array.Empty<BookSuggestion>());
            }

            List<Query> filters = [];
            if (!string.IsNullOrWhiteSpace(category))
            {
                filters.Add(new TermQuery { Field = "categories", Value = category });
            }

            // The token is cancelled when the browser aborts the request (a newer keystroke); the Elasticsearch call stops too.
            SearchResponse<Book> response = await client.SearchAsync<Book>(s => s
                .Size(MaxSuggestions)
                .Source(new SourceConfig(new SourceFilter { Includes = Fields.FromStrings(["isbn", "title"]) }))
                .TrackTotalHits(new TrackHits(false))
                // The catalog has several books with the same title; show each title once.
                .Collapse(new FieldCollapse { Field = "title.keyword" })
                .Query(new BoolQuery
                {
                    Must = [new MatchQuery { Field = "title.autocomplete", Query = q, Operator = Operator.And }],
                    Should = [new MatchPhrasePrefixQuery { Field = "title.folded", Query = q }],
                    Filter = filters
                }), cancellationToken);

            ILogger logger = loggerFactory.CreateLogger("Bookstore.Api.BookSuggestEndpoints");
            if (!response.IsValidResponse)
            {
                logger.LogWarning("Suggest failed: HTTP {StatusCode}", response.ApiCallDetails.HttpStatusCode);
                return Results.Ok(Array.Empty<BookSuggestion>());
            }

            logger.LogInformation("Suggest {Length} chars: {Count} hits, took {Took} ms", q.Length, response.Hits.Count, response.Took);
            return Results.Ok(response.Hits.Select(h => new BookSuggestion(h.Source!.Isbn, h.Source.Title)).ToList());
        })
        .CacheOutput(CachePolicy);

        return app;
    }
}
