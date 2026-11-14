using Bookstore.Search;
using Elastic.Clients.Elasticsearch;

namespace Bookstore.SearchLab;

public static class LabClient
{
    // The lab console reads its settings from environment variables.
    public static ElasticsearchClient Create() => SearchClientFactory.Create(new ElasticsearchOptions
    {
        Url = Environment.GetEnvironmentVariable("BOOKSTORE_ES_URL") ?? "http://localhost:9200",
        ApiKey = Environment.GetEnvironmentVariable("BOOKSTORE_ES_API_KEY") ?? ""
    });
}
