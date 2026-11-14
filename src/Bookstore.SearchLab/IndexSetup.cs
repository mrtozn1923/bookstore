using Bookstore.Search;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using HttpMethod = Elastic.Transport.HttpMethod;

namespace Bookstore.SearchLab;

// Creates the books index from search/books.index.json and loads data/books.json into it.
public static class IndexSetup
{
    public static async Task RunAsync(string[] args)
    {
        string indexName = args.Length > 0 ? args[0] : SearchClientFactory.BooksIndex;
        ElasticsearchClient client = LabClient.Create();

        bool exists = (await client.Indices.ExistsAsync(indexName)).Exists;
        if (exists)
        {
            Console.WriteLine($"Index '{indexName}' already exists. Delete it in Dev Tools (DELETE /{indexName}) to recreate it.");
            return;
        }

        // The mapping lives in a JSON file; the low-level transport sends it as is.
        string definition = await File.ReadAllTextAsync(Path.Combine("search", "books.index.json"));
        StringResponse created = await client.Transport.RequestAsync<StringResponse>(
            HttpMethod.PUT, $"/{indexName}", PostData.String(definition));
        Console.WriteLine($"PUT /{indexName}: HTTP {created.ApiCallDetails.HttpStatusCode} {created.Body}");
        if (!created.ApiCallDetails.HasSuccessfulStatusCode)
        {
            return;
        }

        IReadOnlyList<Book> books = await BookCatalog.LoadAsync(Path.Combine("data", "books.json"));
        BulkResponse bulk = await client.BulkAsync(b => b
            .Index(indexName)
            .IndexMany(books)
            .Refresh(Elastic.Clients.Elasticsearch.Refresh.WaitFor));
        Console.WriteLine($"Bulk: {books.Count} books, errors: {bulk.Errors}, took {bulk.Took} ms");
    }
}
