using System.Net.ServerSentEvents;
using System.Text.Json;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using Bookstore.Api.Assistant;

namespace Bookstore.Api.Tests;

public sealed class ClaudeStreamMapperTests
{
    // The "Basic streaming request" response from the streaming page (ping left out).
    private static readonly string[] DocumentedEvents =
    [
        """{"type": "message_start", "message": {"id": "msg_1nZdL29xx5MUA1yADyHTEsnR8uuvGzszyY", "type": "message", "role": "assistant", "content": [], "model": "claude-opus-5-5", "stop_reason": null, "stop_sequence": null, "usage": {"input_tokens": 25, "output_tokens": 1}}}""",
        """{"type": "content_block_start", "index": 0, "content_block": {"type": "text", "text": ""}}""",
        """{"type": "content_block_delta", "index": 0, "delta": {"type": "text_delta", "text": "Hello"}}""",
        """{"type": "content_block_delta", "index": 0, "delta": {"type": "text_delta", "text": "!"}}""",
        """{"type": "content_block_stop", "index": 0}""",
        """{"type": "message_delta", "delta": {"stop_reason": "end_turn", "stop_sequence":null}, "usage": {"output_tokens": 15}}""",
        """{"type": "message_stop"}""",
    ];

    private static async IAsyncEnumerable<RawMessageStreamEvent> Replay(IEnumerable<string> lines, Exception? failAtEnd = null)
    {
        foreach (string line in lines)
        {
            await Task.Yield();
            yield return JsonSerializer.Deserialize<RawMessageStreamEvent>(line)!;
        }

        if (failAtEnd is not null)
        {
            throw failAtEnd;
        }
    }

    [Fact]
    public async Task MapAsync_DocumentedStream_YieldsTextThenDone()
    {
        List<SseItem<AssistantChunk>> items = [];
        await foreach (SseItem<AssistantChunk> item in ClaudeStreamMapper.MapAsync(Replay(DocumentedEvents)))
        {
            items.Add(item);
        }

        Assert.Equal(["text", "text", "done"], items.Select(i => i.EventType));
        Assert.Equal("Hello!", string.Concat(items.Select(i => i.Data.Text)));
        AssistantChunk done = items[^1].Data;
        Assert.Equal("end_turn", done.StopReason);
        Assert.Equal(25, done.InputTokens);
        Assert.Equal(15, done.OutputTokens);
    }

    [Fact]
    public async Task MapAsync_ErrorInsideTheStream_YieldsAnErrorEvent()
    {
        List<SseItem<AssistantChunk>> items = [];
        await foreach (SseItem<AssistantChunk> item in ClaudeStreamMapper.MapAsync(
            Replay(DocumentedEvents[..3], new AnthropicSseException("overloaded_error"))))
        {
            items.Add(item);
        }

        Assert.Equal(["text", "error"], items.Select(i => i.EventType));
    }
}

public sealed class ToolInputAccumulatorTests
{
    [Fact]
    public void TryComplete_JoinsFragments()
    {
        ToolInputAccumulator accumulator = new();
        accumulator.Append(1, "{\"location\": \"San Fra");
        accumulator.Append(1, "ncisco, CA\"}");

        Assert.True(accumulator.TryComplete(1, out JsonElement input));
        Assert.Equal("San Francisco, CA", input.GetProperty("location").GetString());
    }

    [Fact]
    public void TryComplete_CutOffInput_ReturnsFalse()
    {
        ToolInputAccumulator accumulator = new();
        accumulator.Append(1, "{\"location\": \"San Fra");

        Assert.False(accumulator.TryComplete(1, out _));
    }
}
