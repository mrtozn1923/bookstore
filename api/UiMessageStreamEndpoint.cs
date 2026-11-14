using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;

namespace Bookstore.Assistant.Api;

// AI SDK UI'ın gönderdiği gövde: tüm konuşma, her mesaj "parts" listesiyle
public sealed record UiChatRequest(string Id, List<UiMessage> Messages);
public sealed record UiMessage(string Id, string Role, List<JsonObject> Parts);

public static class UiMessageStreamEndpoint
{
    // Aynı asistan, AI SDK UI message stream (v1) formatıyla
    public static void MapUiMessageStream(this WebApplication app, string systemPrompt)
    {
        app.MapPost("/chat/ui-stream", IResult (UiChatRequest request, HttpContext http, IChatClient chat,
            ILogger<UiChatRequest> logger, CancellationToken ct) =>
        {
            List<ChatMessage> messages = [new(ChatRole.System, systemPrompt)];
            foreach (UiMessage m in request.Messages.Where(m => m.Role is "user" or "assistant"))
            {
                string text = string.Concat(m.Parts
                    .Where(p => (string?)p["type"] == "text")
                    .Select(p => (string?)p["text"]));
                messages.Add(new ChatMessage(m.Role == "user" ? ChatRole.User : ChatRole.Assistant, text));
            }

            // Bu header olmadan istemci akışı UI message stream olarak okumaz
            http.Response.Headers["x-vercel-ai-ui-message-stream"] = "v1";
            return TypedResults.ServerSentEvents(ToParts(chat.GetStreamingResponseAsync(messages, cancellationToken: ct), logger, ct));
        }).RequireRateLimiting("chat");
    }

    private static async IAsyncEnumerable<string> ToParts(IAsyncEnumerable<ChatResponseUpdate> updates, ILogger logger,
        [EnumeratorCancellation] CancellationToken ct)
    {
        static string Part(object part) => JsonSerializer.Serialize(part, JsonSerializerOptions.Web);

        string textId = Guid.NewGuid().ToString("N");
        bool textStarted = false;
        yield return Part(new { type = "start", messageId = Guid.NewGuid().ToString("N") });
        yield return Part(new { type = "start-step" });

        IAsyncEnumerator<ChatResponseUpdate> e = updates.GetAsyncEnumerator(ct);
        try
        {
            while (true)
            {
                string? error = null;
                try
                {
                    if (!await e.MoveNextAsync())
                    {
                        break;
                    }
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    logger.LogWarning(ex, "Model stream failed");
                    error = "The model stopped responding.";
                }
                if (error is not null)
                {
                    yield return Part(new { type = "error", errorText = error });
                    yield break;
                }

                foreach (TextContent text in e.Current.Contents.OfType<TextContent>().Where(t => t.Text.Length > 0))
                {
                    if (!textStarted)
                    {
                        textStarted = true;
                        yield return Part(new { type = "text-start", id = textId });
                    }
                    yield return Part(new { type = "text-delta", id = textId, delta = text.Text });
                }
            }
        }
        finally
        {
            await e.DisposeAsync();
        }

        if (textStarted)
        {
            yield return Part(new { type = "text-end", id = textId });
        }
        yield return Part(new { type = "finish-step" });
        yield return Part(new { type = "finish" });
        yield return "[DONE]";
    }
}
