using Anthropic;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Ai;

public static class ClaudeServiceCollectionExtensions
{
    public static IServiceCollection AddClaude(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ClaudeOptions>()
            .Bind(configuration.GetSection(ClaudeOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // One client for the whole app: it shares the connection pool and the retry settings.
        services.AddSingleton<IAnthropicClient>(sp =>
        {
            ClaudeOptions options = sp.GetRequiredService<IOptions<ClaudeOptions>>().Value;
            return string.IsNullOrWhiteSpace(options.ApiKey)
                ? new AnthropicClient { MaxRetries = options.MaxRetries, Timeout = options.Timeout }
                : new AnthropicClient { ApiKey = options.ApiKey, MaxRetries = options.MaxRetries, Timeout = options.Timeout };
        });

        return services;
    }
}
