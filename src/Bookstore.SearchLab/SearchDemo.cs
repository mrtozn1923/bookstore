using System.Globalization;
using Bookstore.Search;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;

namespace Bookstore.SearchLab;

public static class SearchDemo
{
    // search <text...> [rank-eval options]: runs the same query as the API (BookSearchQuery, post 07).
    public static async Task RunAsync(string[] args)
    {
        string text = string.Join(' ', args.TakeWhile(a => !a.StartsWith("--")));
        SearchTuning tuning = RankEvalDemo.ParseTuning(args.SkipWhile(a => !a.StartsWith("--")).ToArray());
        ElasticsearchClient client = LabClient.Create();

        SearchResponse<Book> response = await client.SearchAsync<Book>(s => s
            .Size(5)
            .Query(BookSearchQuery.Build(new BookSearchFilter(text), tuning)));

        if (!response.IsValidResponse)
        {
            Console.WriteLine($"Search failed: {response.ElasticsearchServerError?.Error?.Reason}");
            return;
        }

        Console.WriteLine($"\"{text}\": {response.Total} hits");
        foreach (Hit<Book> hit in response.Hits)
        {
            Book book = hit.Source!;
            Console.WriteLine($"  {hit.Score?.ToString("F2", CultureInfo.InvariantCulture),6}  {book.Title} ({book.Language}, {book.AverageRating?.ToString(CultureInfo.InvariantCulture)}, {book.PublishedOn.Year})");
        }
    }
}
