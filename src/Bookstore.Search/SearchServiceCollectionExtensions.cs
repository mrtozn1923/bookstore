using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bookstore.Search;

public static class SearchServiceCollectionExtensions
{
    public static IServiceCollection AddBookstoreSearch(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        services.AddOptions<ElasticsearchOptions>()
            .Bind(configuration.GetSection(ElasticsearchOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey), "Elasticsearch:ApiKey is empty.")
            .ValidateOnStart();

        // Singleton: the client keeps the connection pool and node state; never create one per request.
        services.AddSingleton<ElasticsearchClient>(provider =>
        {
            ElasticsearchOptions options = provider.GetRequiredService<IOptions<ElasticsearchOptions>>().Value;
            ILogger? debugLogger = isDevelopment
                ? provider.GetRequiredService<ILoggerFactory>().CreateLogger("Bookstore.Search.Elasticsearch")
                : null;
            return SearchClientFactory.Create(options, debugLogger);
        });

        return services;
    }
}
