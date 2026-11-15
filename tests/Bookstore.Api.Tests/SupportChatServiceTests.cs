using System.Text.Json;
using Anthropic.Models.Messages;
using Bookstore.Api.Ai;
using Bookstore.Api.Support;

namespace Bookstore.Api.Tests;

public sealed class SupportChatServiceTests
{
    [Fact]
    public void BuildRequest_OperatorNoteFollowsTheUserTurn()
    {
        List<MessageParam> history =
        [
            new UserMessageParam { Content = "Where is my order?" },
            new AssistantMessageParam { Content = "Could you share the order number?" },
            new UserMessageParam { Content = "It is 1042." },
            new SystemMessageParam { Content = "This customer has an open complaint. Offer a call back." },
        ];

        MessageCreateParams request = SupportChatService.BuildRequest(history, new ClaudeOptions());
        JsonElement messages = JsonSerializer.SerializeToElement(request.RawBodyData)
            .GetProperty("messages");

        string[] roles = messages.EnumerateArray().Select(m => m.GetProperty("role").GetString()!).ToArray();
        Assert.Equal(["user", "assistant", "user", "system"], roles);
    }
}
