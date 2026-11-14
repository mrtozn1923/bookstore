using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;

namespace Bookstore.Search;

public sealed record BookSearchInput(BookSearchFilter Filter, string? Sort = null, int Page = 1, int PageSize = 20);

// Builds the whole /books/search request (post 10): no HTTP, no client, so it can be unit tested.
public static class BookSearchRequestBuilder
{
    public const int MaxPageSize = 50;
    public const int MaxResultWindow = 10_000;
    public const string DidYouMean = "did-you-mean";
    private const int MinHitsWithoutSuggestion = 3;

    public static SearchRequest<Book> Build(BookSearchInput input, SearchTuning tuning)
    {
        string? text = input.Filter.Text;
        bool hasText = !string.IsNullOrWhiteSpace(text);
        return new SearchRequest<Book>
        {
            From = (input.Page - 1) * input.PageSize,
            Size = input.PageSize,
            TrackTotalHits = new TrackHits(true),
            // Only the text goes into the query, so every facet counts over everything the text matched.
            Query = BookSearchQuery.Build(new BookSearchFilter(text), tuning),
            // Selected filters narrow the hits only, after the aggregations have run.
            PostFilter = BookFacets.PostFilter(input.Filter),
            Aggregations = BookFacets.Aggregations(input.Filter),
            Sort = BuildSort(input.Sort),
            Highlight = hasText ? BuildHighlight() : null,
            Suggest = hasText ? BuildDidYouMean(text!) : null
        };
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
    public static string? ReadDidYouMean(SearchResponse<Book> response, string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || response.Suggest is null || response.Total >= MinHitsWithoutSuggestion)
        {
            return null;
        }
        string? suggestion = response.Suggest.GetPhrase(DidYouMean)?.SelectMany(s => s.Options).Select(o => o.Text).FirstOrDefault();
        return suggestion is not null && !string.Equals(suggestion, text.Trim(), StringComparison.OrdinalIgnoreCase) ? suggestion : null;
    }

}
