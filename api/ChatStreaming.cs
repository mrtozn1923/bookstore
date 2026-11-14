using System.Diagnostics;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Bookstore.Assistant.Api;

public static class ChatStreaming
{
    // Model update'lerini tiplenmiş SSE event'lerine çevirir; hata olursa yarım cevabı "error" event'iyle bildirir
    public static async IAsyncEnumerable<SseItem<object>> ToSseAsync(
        IAsyncEnumerable<ChatResponseUpdate> updates,
        ILogger logger,
        Action<string>? onCompleted = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        int deltas = 0;
        System.Text.StringBuilder answer = new();
        bool thinking = false;
        long? firstDeltaMs = null;
        Dictionary<string, string> toolNames = [];
        Stopwatch sw = Stopwatch.StartNew();
        IAsyncEnumerator<ChatResponseUpdate> e = updates.GetAsyncEnumerator(ct);
        try
        {
            while (true)
            {
                ChatResponseUpdate update;
                string? error = null;
                try
                {
                    if (!await e.MoveNextAsync())
                    {
                        break;
                    }
                    update = e.Current;
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    logger.LogWarning(ex, "Model stream failed after {Deltas} deltas", deltas);
                    error = "The model stopped responding.";
                    update = null!;
                }

                // yield catch bloğunda kullanılamaz; hatayı dışarıda yay
                if (error is not null)
                {
                    yield return new SseItem<object>(new ErrorEvent(error, Partial: deltas > 0), "error");
                    yield break;
                }

                foreach (AIContent content in update.Contents)
                {
                    switch (content)
                    {
                        case TextReasoningContent when !thinking:
                            thinking = true;
                            yield return new SseItem<object>(new { }, "thinking");
                            break;
                        case TextContent { Text.Length: > 0 } text:
                            deltas++;
                            firstDeltaMs ??= sw.ElapsedMilliseconds;
                            answer.Append(text.Text);
                            yield return new SseItem<object>(new DeltaEvent(text.Text), "delta");
                            break;
                        case FunctionCallContent call:
                            toolNames[call.CallId] = call.Name;
                            yield return new SseItem<object>(new ToolEvent(call.CallId, call.Name, "started"), "tool");
                            break;
                        case FunctionResultContent result:
                            // Sonuçta yalnız CallId var; adı started event'inde sakladığımız eşlemeden buluyoruz
                            yield return new SseItem<object>(
                                new ToolEvent(result.CallId, toolNames.GetValueOrDefault(result.CallId, ""), "completed"), "tool");
                            break;
                        case UsageContent usage:
                            yield return new SseItem<object>(
                                new UsageEvent(usage.Details.InputTokenCount, usage.Details.OutputTokenCount), "usage");
                            break;
                    }
                }
            }

            // Yalnız düzgün biten tur geçmişe yazılır; yarım cevap saklanmaz
            onCompleted?.Invoke(answer.ToString());
            yield return new SseItem<object>(new { }, "done");
        }
        finally
        {
            await e.DisposeAsync();
            logger.LogInformation(
                "Stream closed: deltas={Deltas}, firstDelta={FirstDelta} ms, elapsed={Elapsed} ms, aborted={Aborted}",
                deltas, firstDeltaMs, sw.ElapsedMilliseconds, ct.IsCancellationRequested);
        }
    }
}
