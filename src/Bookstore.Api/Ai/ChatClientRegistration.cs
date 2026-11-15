using Anthropic;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Ai;

public static class ChatClientRegistration
{
    // Provider-independent features use IChatClient; Claude-specific ones (caching breakpoints, effort, skills, batches)
    // keep using IAnthropicClient directly.
    public static IServiceCollection AddClaudeChatClient(this IServiceCollection services)
    {
        services.AddChatClient(sp =>
            {
                ClaudeOptions options = sp.GetRequiredService<IOptions<ClaudeOptions>>().Value;
                return sp.GetRequiredService<IAnthropicClient>().AsIChatClient(options.Model);
            })
            .UseFunctionInvocation()
            .UseOpenTelemetry(sourceName: "Bookstore.ChatClient");

        return services;
    }
}
