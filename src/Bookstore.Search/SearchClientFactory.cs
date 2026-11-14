using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Serialization;
using Elastic.Transport;
using Microsoft.Extensions.Logging;

namespace Bookstore.Search;

public sealed class ElasticsearchOptions
{
    public const string SectionName = "Elasticsearch";

    public string Url { get; set; } = "http://localhost:9200";

    public string ApiKey { get; set; } = "";

    // Null keeps the client default; the API sets a short timeout (post 10).
    public TimeSpan? RequestTimeout { get; set; }
}

public static class SearchClientFactory
{
    public const string BooksIndex = "books";

    // One client per application: it owns the connection pool and is thread-safe.
    public static ElasticsearchClient Create(ElasticsearchOptions options, ILogger? debugLogger = null)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException("Elasticsearch:ApiKey is empty. Create an API key (post 02) and set it.");
        }

        ElasticsearchClientSettings settings = new ElasticsearchClientSettings(new SingleNodePool(new Uri(options.Url)), CreateSourceSerializer)
            .Authentication(new ApiKey(options.ApiKey))
            .DefaultMappingFor<Book>(m => m.IndexName(BooksIndex).IdProperty(b => b.Isbn));

        if (options.RequestTimeout is TimeSpan timeout)
        {
            settings.RequestTimeout(timeout);
        }

        if (debugLogger is not null)
        {
            // Development only: buffers every request and response body and logs them.
            settings.EnableDebugMode(details => debugLogger.LogDebug("{DebugInformation}", details.DebugInformation));
        }

        return new ElasticsearchClient(settings);
    }

    // Documents (_source) are serialized with System.Text.Json; enums are written in lowercase kebab case ("e-book").
    // Insert, not Add: the default options already contain an enum converter, and the first matching converter wins.
    public static Serializer CreateSourceSerializer(Serializer builtIn, IElasticsearchClientSettings settings) =>
        new DefaultSourceSerializer(settings, options => options.Converters.Insert(0, new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower)));
}
