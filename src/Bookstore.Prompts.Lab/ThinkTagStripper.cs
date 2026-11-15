using Microsoft.Extensions.AI;

namespace Bookstore.Prompts.Lab;

// qwen3:4b on this Ollama build writes its reasoning into the answer even with thinking off;
// everything up to and including </think> is removed before anyone reads the text
public sealed class ThinkTagStripper(IChatClient inner) : DelegatingChatClient(inner)
{
    const string EndTag = "</think>";

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        ChatResponse response = await base.GetResponseAsync(messages, options, cancellationToken);
        foreach (TextContent text in response.Messages.SelectMany(m => m.Contents).OfType<TextContent>())
        {
            int end = text.Text.IndexOf(EndTag, StringComparison.Ordinal);
            if (end >= 0)
            {
                text.Text = text.Text[(end + EndTag.Length)..].TrimStart();
            }
        }
        return response;
    }
}
