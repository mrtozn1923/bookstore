using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;

namespace Bookstore.Api.Assistant;

public static class ClaudeStreamMapper
{
    public const string TextEvent = "text";
    public const string ThinkingEvent = "thinking";
    public const string DoneEvent = "done";
    public const string ErrorEvent = "error";

    public static async IAsyncEnumerable<SseItem<AssistantChunk>> MapAsync(
        IAsyncEnumerable<RawMessageStreamEvent> events,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        long inputTokens = 0;
        long outputTokens = 0;
        string? stopReason = null;
        bool thinkingAnnounced = false;

        await using IAsyncEnumerator<RawMessageStreamEvent> enumerator = events.GetAsyncEnumerator(cancellationToken);
        while (true)
        {
            bool hasNext;
            bool interrupted = false;
            try
            {
                hasNext = await enumerator.MoveNextAsync();
            }
            catch (Exception ex) when (ex is AnthropicSseException or AnthropicIOException)
            {
                // HTTP status was 200; the error came inside the stream (overloaded_error) or the connection dropped.
                hasNext = false;
                interrupted = true;
            }

            if (interrupted)
            {
                yield return new SseItem<AssistantChunk>(new AssistantChunk(Error: "The answer was interrupted."), ErrorEvent);
                yield break;
            }

            if (!hasNext)
            {
                break;
            }

            RawMessageStreamEvent current = enumerator.Current;
            if (current.TryPickStart(out RawMessageStartEvent? start))
            {
                inputTokens = start.Message.Usage.InputTokens;
            }
            else if (current.TryPickContentBlockDelta(out RawContentBlockDeltaEvent? delta))
            {
                if (delta.Delta.TryPickText(out TextDelta? text))
                {
                    yield return new SseItem<AssistantChunk>(new AssistantChunk(Text: text.Text), TextEvent);
                }
                else if (delta.Delta.TryPickThinking(out ThinkingDelta? _) && !thinkingAnnounced)
                {
                    // The thinking text is not forwarded; the UI only shows a "thinking" indicator once.
                    thinkingAnnounced = true;
                    yield return new SseItem<AssistantChunk>(new AssistantChunk(), ThinkingEvent);
                }
            }
            else if (current.TryPickDelta(out RawMessageDeltaEvent? messageDelta))
            {
                // Usage in message_delta is cumulative: keep the last value, do not add them up.
                outputTokens = messageDelta.Usage.OutputTokens;
                stopReason = messageDelta.Delta.StopReason?.Raw();
            }
            else if (current.TryPickStop(out RawMessageStopEvent? _))
            {
                yield return new SseItem<AssistantChunk>(
                    new AssistantChunk(StopReason: stopReason, InputTokens: inputTokens, OutputTokens: outputTokens), DoneEvent);
            }

            // ping and event types this SDK version does not know are skipped.
        }
    }
}
