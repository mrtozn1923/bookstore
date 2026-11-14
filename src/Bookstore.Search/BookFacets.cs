using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace Bookstore.Search;

public sealed record FacetValue(string Key, long Count);

public sealed record Facet(string Name, IReadOnlyList<FacetValue> Values);

// Faceted search (post 09): the text query feeds every facet, selected filters go to post_filter,
// and each facet is filtered by every selected filter except its own.
public static class BookFacets
{
    public const string Category = "category";
    public const string Language = "language";
    public const string Price = "price";
    public const string Rating = "rating";
    private const string Values = "values";

    public static Dictionary<string, Query> SelectedFilters(BookSearchFilter filter)
    {
        Dictionary<string, Query> filters = [];
        if (!string.IsNullOrWhiteSpace(filter.Category))
        {
            filters[Category] = new TermQuery { Field = "categories", Value = filter.Category };
        }
        if (!string.IsNullOrWhiteSpace(filter.Language))
        {
            filters[Language] = new TermQuery { Field = "language", Value = filter.Language };
        }
        if (filter.MinPrice is not null || filter.MaxPrice is not null)
        {
            filters[Price] = new NumberRangeQuery
            {
                Field = "price",
                Gte = filter.MinPrice is null ? null : (double)filter.MinPrice.Value,
                Lt = filter.MaxPrice is null ? null : (double)filter.MaxPrice.Value
            };
        }
        if (filter.MinRating is not null)
        {
            filters[Rating] = new NumberRangeQuery { Field = "averageRating", Gte = (double)filter.MinRating.Value };
        }
        return filters;
    }

    public static Query? PostFilter(BookSearchFilter filter)
    {
        Dictionary<string, Query> selected = SelectedFilters(filter);
        if (selected.Count == 0)
        {
            // Not "cond ? null : new BoolQuery()": that converts a null BoolQuery into a Query, sent as an empty {} clause.
            return null;
        }
        return new BoolQuery { Filter = [.. selected.Values] };
    }

    public static Dictionary<string, Aggregation> Aggregations(BookSearchFilter filter)
    {
        Dictionary<string, Query> selected = SelectedFilters(filter);
        return new Dictionary<string, Aggregation>
        {
            [Category] = Facet(selected, Category, new Aggregation { Terms = new TermsAggregation { Field = "categories", Size = 20 } }),
            [Language] = Facet(selected, Language, new Aggregation { Terms = new TermsAggregation { Field = "language", Size = 5 } }),
            [Price] = Facet(selected, Price, new Aggregation
            {
                Range = new RangeAggregation
                {
                    Field = "price",
                    Ranges =
                    [
                        new AggregationRange { Key = "0-200", To = 200 },
                        new AggregationRange { Key = "200-400", From = 200, To = 400 },
                        new AggregationRange { Key = "400-700", From = 400, To = 700 },
                        new AggregationRange { Key = "700+", From = 700 }
                    ]
                }
            }),
            [Rating] = Facet(selected, Rating, new Aggregation
            {
                Range = new RangeAggregation
                {
                    Field = "averageRating",
                    Ranges = [new AggregationRange { Key = "4+", From = 4 }, new AggregationRange { Key = "3+", From = 3 }]
                }
            })
        };
    }

    // A facet sees every selected filter except its own, so its other options keep their counts.
    private static Aggregation Facet(Dictionary<string, Query> selected, string name, Aggregation values)
    {
        List<Query> others = [.. selected.Where(f => f.Key != name).Select(f => f.Value)];
        return new Aggregation
        {
            Filter = new BoolQuery { Filter = others },
            Aggregations = new Dictionary<string, Aggregation> { [Values] = values }
        };
    }

    public static IReadOnlyList<Facet> Read(AggregateDictionary? aggregations)
    {
        if (aggregations is null)
        {
            return [];
        }

        List<Facet> facets = [];
        foreach (string name in new[] { Category, Language })
        {
            StringTermsAggregate? terms = aggregations.GetFilter(name)?.Aggregations?.GetStringTerms(Values);
            facets.Add(new Facet(name, terms?.Buckets.Select(b => new FacetValue(b.Key.ToString(), b.DocCount)).ToList() ?? []));
        }
        foreach (string name in new[] { Price, Rating })
        {
            RangeAggregate? ranges = aggregations.GetFilter(name)?.Aggregations?.GetRange(Values);
            facets.Add(new Facet(name, ranges?.Buckets.Select(b => new FacetValue(b.Key ?? "", b.DocCount)).ToList() ?? []));
        }
        return facets;
    }
}
