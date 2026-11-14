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
    private const int MaxPageSize = 50;
    private const int MaxResultWindow = 10_000;
    private const string DidYouMean = "did-you-mean";
    private const int MinHitsWithoutSuggestion = 3;

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

            BookSearchFilter filter = new(q, category, language, minPrice, maxPrice, minRating);
            SearchRequest<Book> request = new()
            {
                From = from,
                Size = size,
                TrackTotalHits = new TrackHits(true),
                // Only the text goes into the query, so every facet counts over everything the text matched.
                Query = BookSearchQuery.Build(new BookSearchFilter(q), SearchTuning.Tuned),
                // Selected filters narrow the hits only, after the aggregations have run.
                PostFilter = BookFacets.PostFilter(filter),
                Aggregations = BookFacets.Aggregations(filter),
                Sort = BuildSort(sort),
                Highlight = string.IsNullOrWhiteSpace(q) ? null : BuildHighlight(),
                Suggest = string.IsNullOrWhiteSpace(q) ? null : BuildDidYouMean(q)
            };

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
            return Results.Ok(new BookSearchResult(response.Total, currentPage, size, items, ReadDidYouMean(response, q),
                BookFacets.Read(response.Aggregations)));
        });

        return app;
    }

    // isbn is the tie-breaker: equal prices or dates must not reorder between pages.
    private static ICollection<SortOptions> BuildSort(string? sort) => sort switch
    {
        "price_asc" => [By("price", SortOrder.Asc), By("isbn", SortOrder.Asc)],
        "price_desc" => [By("price", SortOrder.Desc), By("isbn", SortOrder.Asc)],
        "newest" => [By("publishedOn", SortOrder.Desc), By("isbn", SortOrder.Asc)],
        _ => [new SortOptions { Score = new ScoreSort { Order = SortOrder.Desc } }, By("isbn", SortOrder.Asc)]
    };

    private static SortOptions By(string field, SortOrder order) => new() { Field = new FieldSort { Field = field, Order = order } };

    private static Highlight BuildHighlight() => new()
    {
        Encoder = HighlighterEncoder.Html,
        PreTags = ["<mark>"],
        PostTags = ["</mark>"],
        Fields = new Dictionary<Field, HighlightField>
        {
            // The query hits title.folded / title.tr too; matched_fields maps those hits back onto the original title.
            ["title"] = new HighlightField { NumberOfFragments = 0, MatchedFields = Fields.FromStrings(["title", "title.folded", "title.tr"]) },
            ["description"] = new HighlightField { NumberOfFragments = 1, FragmentSize = 160, MatchedFields = Fields.FromStrings(["description", "description.folded", "description.tr"]) }
        }
    };

    // "Did you mean?": a phrase suggester on the folded title; collate drops suggestions that would find nothing.
    private static Suggester BuildDidYouMean(string text) => new()
    {
        Text = text,
        Suggesters = new Dictionary<string, FieldSuggester>
        {
            [DidYouMean] = new FieldSuggester { Phrase = new PhraseSuggester
            {
                Field = "title.folded",
                Size = 1,
                DirectGenerator = [new DirectGenerator { Field = "title.folded", SuggestMode = SuggestMode.Always }],
                Collate = new PhraseSuggestCollate
                {
                    Query = new PhraseSuggestCollateQuery { Source = """{ "match": { "title.folded": { "query": "{{suggestion}}", "operator": "and" } } }""" },
                    Prune = false
                }
            } }
        }
    };

    // Fuzziness already finds most typos; suggest only when the search found little (post 09: "roman" got "road").
    private static string? ReadDidYouMean(SearchResponse<Book> response, string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || response.Suggest is null || response.Total >= MinHitsWithoutSuggestion)
        {
            return null;
        }
        string? suggestion = response.Suggest.GetPhrase(DidYouMean)?.SelectMany(s => s.Options).Select(o => o.Text).FirstOrDefault();
        return suggestion is not null && !string.Equals(suggestion, text.Trim(), StringComparison.OrdinalIgnoreCase) ? suggestion : null;
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
