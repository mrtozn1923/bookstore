using System.Text.Json;
using Anthropic.Models.Messages;
using Bookstore.Api.Ai;

namespace Bookstore.Api.Tests;

public sealed class ClaudeReplyReaderTests
{
    private static Message Parse(string json) => JsonSerializer.Deserialize<Message>(json)!;

    [Fact]
    public void Read_DocumentedRefusal_ReturnsRefusedWithCategory()
    {
        // The refusal example from the "Refusals and fallback" page.
        Message message = Parse("""
            {"id":"msg_01XFUDYJgAACzvnptvVoYEL","type":"message","role":"assistant","model":"claude-fable-5","content":[],
             "stop_reason":"refusal",
             "stop_details":{"type":"refusal","category":"cyber","explanation":"This request was declined because it could enable cyber harm."},
             "usage":{"input_tokens":412,"output_tokens":0}}
            """);

        ClaudeReply reply = ClaudeReplyReader.Read(message);

        Assert.Equal(ReplyOutcome.Refused, reply.Outcome);
        Assert.Equal("cyber", reply.RefusalCategory);
        Assert.Equal(412, reply.Usage.Input);
        Assert.Empty(reply.Text);
    }

    [Fact]
    public void Read_ThinkingBeforeText_ReturnsOnlyTheText()
    {
        Message message = Parse("""
            {"id":"msg_test","type":"message","role":"assistant","model":"claude-haiku-5-5",
             "content":[{"type":"thinking","thinking":"","signature":"sig"},{"type":"text","text":"Your order ships tomorrow."}],
             "stop_reason":"end_turn","usage":{"input_tokens":120,"output_tokens":45,"output_tokens_details":{"thinking_tokens":30}}}
            """);

        ClaudeReply reply = ClaudeReplyReader.Read(message);

        Assert.Equal(ReplyOutcome.Complete, reply.Outcome);
        Assert.Equal("Your order ships tomorrow.", reply.Text);
        Assert.Equal(30, reply.Usage.Thinking);
        Assert.Equal(2, reply.AssistantContent.Count);
    }

    [Fact]
    public void Read_MaxTokens_ReturnsTruncated()
    {
        Message message = Parse("""
            {"id":"msg_test","type":"message","role":"assistant","model":"claude-haiku-5-5",
             "content":[{"type":"text","text":"Returns are accepted within"}],
             "stop_reason":"max_tokens","usage":{"input_tokens":200,"output_tokens":2048}}
            """);

        Assert.Equal(ReplyOutcome.Truncated, ClaudeReplyReader.Read(message).Outcome);
    }

    [Fact]
    public void Read_UnknownStopReason_ReturnsUnknown()
    {
        Message message = Parse("""
            {"id":"msg_test","type":"message","role":"assistant","model":"claude-haiku-5-5","content":[],
             "stop_reason":"a_future_reason","usage":{"input_tokens":1,"output_tokens":0}}
            """);

        Assert.Equal(ReplyOutcome.Unknown, ClaudeReplyReader.Read(message).Outcome);
    }
}
