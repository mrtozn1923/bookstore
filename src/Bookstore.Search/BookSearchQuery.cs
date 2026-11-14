using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace Bookstore.Search;

public sealed record BookSearchFilter(string? Text, string? Category = null, string? Language = null, decimal? MinPrice = null, decimal? MaxPrice = null,
    decimal? MinRating = null);

// Every knob that changes ranking, in one place, so _rank_eval and the API run the same query.
public sealed record SearchTuning
{
    public required IReadOnlyList<string> TextFields { get; init; }

    public TextQueryType Type { get; init; } = TextQueryType.BestFields;

    public double? TieBreaker { get; init; }

    public IReadOnlyList<string> AuthorFields { get; init; } = ["authors.name"];

    public float AuthorBoost { get; init; } = 1;

    public string? Fuzziness { get; init; }

    public float? PhraseBoost { get; init; }

    public bool RatingAndRecency { get; init; }

    // The guessed weights from post 04/06.
    public static SearchTuning Baseline { get; } = new()
    {
        TextFields = ["title^3", "title.tr^2", "title.folded^2", "title.en", "description", "description.tr", "description.folded", "description.en"]
    };

    // Measured with rank-eval (post 07): nDCG@5 0.905 -> 0.979 on search/relevance/judgments.json.
    public static SearchTuning Tuned { get; } = Baseline with
    {
        AuthorFields = ["authors.name", "authors.name.folded"],
        AuthorBoost = 3,
        Fuzziness = "AUTO",
        PhraseBoost = 2,
        RatingAndRecency = true
    };
}

public static class BookSearchQuery
{
    public static Query Build(BookSearchFilter filter, SearchTuning tuning)
    {
        // Query context: what the user typed decides the score.
        Query textQuery = string.IsNullOrWhiteSpace(filter.Text)
            ? new MatchAllQuery()
            : new BoolQuery { Should = TextClauses(filter.Text, tuning), MinimumShouldMatch = 1 };

        if (tuning.RatingAndRecency && !string.IsNullOrWhiteSpace(filter.Text))
        {
            textQuery = WithRatingAndRecency(textQuery);
        }

        // Filter context: yes/no conditions, no score, cacheable.
        List<Query> filters = [.. BookFacets.SelectedFilters(filter).Values];
        return new BoolQuery { Must = [textQuery], Filter = filters };
    }

    private static List<Query> TextClauses(string text, SearchTuning tuning)
    {
        Fuzziness? fuzziness = tuning.Fuzziness is null ? null : new Fuzziness(tuning.Fuzziness);
        int? prefixLength = fuzziness is null ? null : 1;
        List<Query> clauses =
        [
            new MultiMatchQuery
            {
                Query = text,
                Fields = Fields.FromStrings(tuning.TextFields.ToArray()),
                Type = tuning.Type,
                TieBreaker = tuning.TieBreaker,
                Fuzziness = fuzziness,
                PrefixLength = prefixLength
            },
            // Authors are nested (post 03): a root-level multi_match cannot see them.
            new NestedQuery
            {
                Path = "authors",
                ScoreMode = ChildScoreMode.Max,
                Boost = tuning.AuthorBoost,
                Query = new MultiMatchQuery
                {
                    Query = text,
                    Fields = Fields.FromStrings(tuning.AuthorFields.ToArray()),
                    Fuzziness = fuzziness,
                    PrefixLength = prefixLength
                }
            }
        ];

        if (tuning.PhraseBoost is float phraseBoost)
        {
            // The user's words in the same order in the title: an extra should clause, never a requirement.
            clauses.Add(new MatchPhraseQuery { Field = "title.folded", Query = text, Slop = 1, Boost = phraseBoost });
        }

        return clauses;
    }

    // Text relevance stays the main signal; rating and recency only nudge it.
    private static Query WithRatingAndRecency(Query textQuery) => new FunctionScoreQuery
    {
        Query = textQuery,
        Functions =
        [
            new FunctionScore
            {
                FieldValueFactor = new FieldValueFactorScoreFunction
                {
                    Field = "averageRating",
                    Modifier = FieldValueFactorModifier.Log1p,
                    Missing = 3
                }
            },
            new FunctionScore
            {
                Weight = 0.5,
                Gauss = new DateDecayFunction
                {
                    Field = "publishedOn",
                    Placement = new DecayPlacement<DateMath, Duration> { Origin = DateMath.Now, Scale = "3650d", Offset = "365d", Decay = 0.5 }
                }
            }
        ],
        ScoreMode = FunctionScoreMode.Sum,
        BoostMode = FunctionBoostMode.Multiply
    };
}
