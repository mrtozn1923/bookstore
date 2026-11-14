using Bookstore.Search;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

namespace Bookstore.SearchLab;

public static class LabClient
{
    // The lab console reads its settings from environment variables.
    public static ElasticsearchOptions Options => new()
    {
        Url = Environment.GetEnvironmentVariable("BOOKSTORE_ES_URL") ?? "http://localhost:9200",
        ApiKey = Environment.GetEnvironmentVariable("BOOKSTORE_ES_API_KEY") ?? "",
        CertificateFingerprint = Environment.GetEnvironmentVariable("BOOKSTORE_ES_CA_FINGERPRINT")
    };

    public static ElasticsearchClient Create() => SearchClientFactory.Create(Options);

    // Post 11: index templates, reindex and alias swaps run with a separate admin key (BOOKSTORE_ES_ADMIN_API_KEY);
    // the application's key cannot manage aliases or delete indices.
    public static ElasticsearchClient CreateAdmin() => SearchClientFactory.Create(new ElasticsearchOptions
    {
        Url = Options.Url,
        ApiKey = Environment.GetEnvironmentVariable("BOOKSTORE_ES_ADMIN_API_KEY") ?? "",
        CertificateFingerprint = Options.CertificateFingerprint
    });

    // debug: keeps request/response bytes (post 10); onRequestCompleted sees every call.
    public static ElasticsearchClient Create(bool debug, Action<ApiCallDetails>? onRequestCompleted = null)
    {
        if (!debug)
        {
            return Create();
        }

        ElasticsearchClientSettings settings = new ElasticsearchClientSettings(new SingleNodePool(new Uri(Options.Url)), SearchClientFactory.CreateSourceSerializer)
            .Authentication(new ApiKey(Options.ApiKey))
            .DefaultMappingFor<Book>(m => m.IndexName(SearchClientFactory.BooksIndex).IdProperty(b => b.Isbn))
            .DisableDirectStreaming();
        if (onRequestCompleted is not null)
        {
            settings.OnRequestCompleted(onRequestCompleted);
        }
        return new ElasticsearchClient(settings);
    }
}
