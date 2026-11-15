using System.Net.ServerSentEvents;
using Anthropic;
using Anthropic.Models.Messages;
using Bookstore.Api.Ai;
using Bookstore.Api.Catalog;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Assistant;

public sealed class ShoppingAssistant(IAnthropicClient claude, IBookRepository books, IOptions<ClaudeOptions> options)
{
    public MessageCreateParams BuildRequest(string question) => new()
    {
        Model = options.Value.Model,
        MaxTokens = 4096,
        System = $"""
            You are the shopping assistant of Bookstore, an online bookstore. Answer in the customer's language.
            Recommend only books from this catalog, with their prices in TRY:
            {string.Join("\n", books.Search(string.Empty, null).Select(b => $"- {b.Title} by {b.Author}, {b.Price} TRY"))}
            """,
        Messages = [new UserMessageParam { Content = question }],
    };

    public IAsyncEnumerable<SseItem<AssistantChunk>> StreamAsync(string question, CancellationToken cancellationToken) =>
        ClaudeStreamMapper.MapAsync(claude.Messages.CreateStreaming(BuildRequest(question), cancellationToken), cancellationToken);
}
