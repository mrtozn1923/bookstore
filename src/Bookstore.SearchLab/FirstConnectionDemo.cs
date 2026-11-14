using Bookstore.Search;
using Elastic.Clients.Elasticsearch;

namespace Bookstore.SearchLab;

public static class FirstConnectionDemo
{
    public static async Task RunAsync()
    {
        ElasticsearchOptions options = new()
        {
            Url = Environment.GetEnvironmentVariable("BOOKSTORE_ES_URL") ?? "http://localhost:9200",
            ApiKey = Environment.GetEnvironmentVariable("BOOKSTORE_ES_API_KEY") ?? ""
        };
        ElasticsearchClient client = SearchClientFactory.Create(options);

        InfoResponse info = await client.InfoAsync();
        Console.WriteLine(info.IsValidResponse
            ? $"Connected: {info.Name}, Elasticsearch {info.Version.Number}"
            : $"Info failed: HTTP {info.ApiCallDetails.HttpStatusCode}, {info.ElasticsearchServerError?.Error?.Type}");

        IReadOnlyList<Book> books = await BookCatalog.LoadAsync(Path.Combine("data", "books.json"));
        Book dune = books.First(b => b.Title == "Dune" && b.Language == "en");

        IndexResponse indexed = await client.IndexAsync(dune);
        Console.WriteLine($"Index: {indexed.Result}, _id {indexed.Id}, _version {indexed.Version}, _seq_no {indexed.SeqNo}");

        GetResponse<Book> got = await client.GetAsync<Book>(dune.Isbn);
        Console.WriteLine(got.Found
            ? $"Get: {got.Source!.Title} ({got.Source.Language}), {got.Source.Authors[0].Name}, {got.Source.Price} {got.Source.Currency}, published {got.Source.PublishedOn:yyyy-MM-dd}"
            : "Get: not found");

        GetResponse<Book> missing = await client.GetAsync<Book>("9780000000000");
        Console.WriteLine($"Missing: IsValidResponse {missing.IsValidResponse}, Found {missing.Found}, HTTP {missing.ApiCallDetails.HttpStatusCode}");
    }
}
