using System.Globalization;
using System.Text.Json;
using Bookstore.Search;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.RankEval;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace Bookstore.SearchLab;

// Scores the search query against search/relevance/judgments.json with the _rank_eval API (nDCG@5).
public static class RankEvalDemo
{
    private sealed record Judgment(string Query, Dictionary<string, int> Ratings);

    public static async Task RunAsync(string[] args)
    {
        SearchTuning tuning = ParseTuning(args);
        List<Judgment> judgments = JsonSerializer.Deserialize<List<Judgment>>(
            await File.ReadAllTextAsync(Path.Combine("search", "relevance", "judgments.json")), BookCatalog.JsonOptions) ?? [];

        RankEvalRequest request = new()
        {
            Indices = SearchClientFactory.BooksIndex,
            Metric = new RankEvalMetric { Dcg = new RankEvalMetricDiscountedCumulativeGain { K = 5, Normalize = true } },
            Requests = judgments.Select(j => new RankEvalRequestItem
            {
                Id = j.Query,
                Request = new RankEvalQuery { Query = BookSearchQuery.Build(new BookSearchFilter(j.Query), tuning), Size = 5 },
                Ratings = j.Ratings.Select(r => new DocumentRating { Index = SearchClientFactory.BooksIndex, Id = r.Key, Rating = r.Value }).ToList()
            }).ToList()
        };

        RankEvalResponse response = await LabClient.Create().RankEvalAsync(request);
        if (!response.IsValidResponse)
        {
            Console.WriteLine($"rank_eval failed: {response.ElasticsearchServerError?.Error?.Reason ?? response.DebugInformation}");
            return;
        }

        Console.WriteLine($"nDCG@5 {response.MetricScore.ToString("F3", CultureInfo.InvariantCulture)}");
        foreach ((string query, RankEvalMetricDetail detail) in response.Details.OrderBy(d => d.Value.MetricScore))
        {
            if (detail.MetricScore < 0.999)
            {
                string top = string.Join(", ", detail.Hits.Take(3).Select(h => h.Hit.Id + (h.Rating is null ? "(-)" : $"({h.Rating})")));
                Console.WriteLine($"  {detail.MetricScore.ToString("F3", CultureInfo.InvariantCulture)}  {query,-26} top: {top}");
            }
        }
    }

    // rank-eval [--type best_fields|most_fields|cross_fields] [--tie 0.3] [--fields a^3,b] [--author-fields a,b] [--author-boost 2] [--fuzzy AUTO] [--phrase 2] [--signals] [--tuned]
    public static SearchTuning ParseTuning(string[] args)
    {
        SearchTuning tuning = SearchTuning.Baseline;
        for (int i = 0; i < args.Length; i++)
        {
            tuning = args[i] switch
            {
                "--type" => tuning with { Type = args[++i] switch { "most_fields" => TextQueryType.MostFields, "cross_fields" => TextQueryType.CrossFields, _ => TextQueryType.BestFields } },
                "--tie" => tuning with { TieBreaker = double.Parse(args[++i], CultureInfo.InvariantCulture) },
                "--fields" => tuning with { TextFields = args[++i].Split(',') },
                "--author-fields" => tuning with { AuthorFields = args[++i].Split(',') },
                "--author-boost" => tuning with { AuthorBoost = float.Parse(args[++i], CultureInfo.InvariantCulture) },
                "--fuzzy" => tuning with { Fuzziness = args[++i] },
                "--phrase" => tuning with { PhraseBoost = float.Parse(args[++i], CultureInfo.InvariantCulture) },
                "--signals" => tuning with { RatingAndRecency = true },
                "--tuned" => SearchTuning.Tuned,
                _ => throw new ArgumentException($"Unknown option {args[i]}")
            };
        }
        return tuning;
    }
}
