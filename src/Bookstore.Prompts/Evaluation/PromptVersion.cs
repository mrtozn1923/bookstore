using Microsoft.Extensions.AI;

namespace Bookstore.Prompts.Evaluation;

// A prompt is versioned together with the output format it expects
public sealed record PromptVersion(
    string Task,
    string Version,
    string System,
    Func<string, string> BuildUser,
    ChatResponseFormat? Format = null,
    string? Fingerprint = null)
{
    public List<ChatMessage> Render(string input) =>
        [new(ChatRole.System, System), new(ChatRole.User, BuildUser(input))];
}
