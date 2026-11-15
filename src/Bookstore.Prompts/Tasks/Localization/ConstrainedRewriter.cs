using Bookstore.Prompts.Evaluation;
using Microsoft.Extensions.AI;

namespace Bookstore.Prompts.Tasks.Localization;

public sealed record RewriteResult(string Text, int Attempts, List<string> Issues);

// Code measures, the model rewrites: failed checks go back as feedback
public sealed class ConstrainedRewriter(IChatClient client, ChatOptions options)
{
    public async Task<RewriteResult> RewriteAsync(
        PromptVersion prompt,
        string input,
        Func<string, List<string>> check,
        int maxAttempts = 3,
        CancellationToken cancellationToken = default)
    {
        List<ChatMessage> messages = prompt.Render(input);
        string text = "";
        List<string> issues = [];
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            ChatResponse response = await client.GetResponseAsync(messages, options, cancellationToken);
            text = response.Text.Trim();
            issues = check(text);
            if (issues.Count == 0)
            {
                return new RewriteResult(text, attempt, issues);
            }
            messages.Add(new ChatMessage(ChatRole.Assistant, text));
            messages.Add(new ChatMessage(ChatRole.User,
                $"Your text has {text.Length} characters. Fix these problems and output only the new text: {string.Join("; ", issues)}"));
        }
        return new RewriteResult(text, maxAttempts, issues);
    }
}
