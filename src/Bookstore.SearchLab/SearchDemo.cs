using System.Globalization;
using Bookstore.Search;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;

namespace Bookstore.SearchLab;

public static class SearchDemo
{
    // Every analyzed variant of title and description; the raw title weighs the most.
    private static readonly string[] SearchFields =
    [
        "title^3", "title.tr^2", "title.folded^2", "title.en",
        "description", "description.tr", "description.folded", "description.en"
    ];

    public static async Task RunAsync(string[] args)
    {
        string text = string.Join(' ', args);
        ElasticsearchClient client = LabClient.Create();

        SearchResponse<Book> response = await client.SearchAsync<Book>(s => s
            .Size(5)
            .Query(q => q.MultiMatch(m => m
                .Query(text)
                .Fields(Fields.FromStrings(SearchFields)))));

        if (!response.IsValidResponse)
        {
            Console.WriteLine($"Search failed: {response.ElasticsearchServerError?.Error?.Reason}");
            return;
        }

        Console.WriteLine($"\"{text}\": {response.Total} hits");
        foreach (Hit<Book> hit in response.Hits)
        {
            Console.WriteLine($"  {hit.Score?.ToString("F2", CultureInfo.InvariantCulture),6}  {hit.Source!.Title} ({hit.Source.Language})");
        }
    }
}
