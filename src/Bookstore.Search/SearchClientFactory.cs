using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

namespace Bookstore.Search;

public sealed class ElasticsearchOptions
{
    public const string SectionName = "Elasticsearch";

    public string Url { get; set; } = "http://localhost:9200";

    public string ApiKey { get; set; } = "";
}

public static class SearchClientFactory
{
    public const string BooksIndex = "books";

    // One client per application: it owns the connection pool and is thread-safe.
    public static ElasticsearchClient Create(ElasticsearchOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException("Elasticsearch:ApiKey is empty. Create an API key (post 02) and set it.");
        }

        ElasticsearchClientSettings settings = new ElasticsearchClientSettings(new Uri(options.Url))
            .Authentication(new ApiKey(options.ApiKey))
            .DefaultMappingFor<Book>(m => m.IndexName(BooksIndex).IdProperty(b => b.Isbn));

        return new ElasticsearchClient(settings);
    }
}
