using System.Runtime.CompilerServices;
using AGUI.Abstractions;
using AGUI.Server;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Bookstore.Assistant.Api;

public static class AgUiEndpoint
{
    // Aynı asistan, AG-UI protokolüyle: istek RunAgentInput, cevap AG-UI event'lerinden oluşan bir SSE akışı
    public static void MapAgUi(this WebApplication app, string systemPrompt)
    {
        app.MapPost("/agui", IResult (RunAgentInput input, IChatClient chat, AssistantTools tools,
            IOptions<JsonOptions> json, ILogger<AgUiRun> logger, CancellationToken ct) =>
        {
            ChatRequestContext context = input.ToChatRequestContext(json.Value.SerializerOptions);
            // Geçmişi istemci gönderir; system prompt ve tool'lar yine yalnız sunucuda
            List<ChatMessage> messages = [new(ChatRole.System, systemPrompt), .. context.Messages];
            ChatOptions options = tools.CreateOptions();
            options.Tools = [.. options.Tools!.Where(t => t.Name == "show_books")];

            IAsyncEnumerable<BaseEvent> events = chat.GetStreamingResponseAsync(messages, options, ct)
                .AsAGUIEventStreamAsync(context, ct);
            return TypedResults.ServerSentEvents(ForBrowser(events, logger, ct));
        }).RequireRateLimiting("chat");
    }

    // Tarayıcıya gidecek event'leri süzer ve run'ın nasıl kapandığını log'a yazar
    private static async IAsyncEnumerable<BaseEvent> ForBrowser(IAsyncEnumerable<BaseEvent> events, ILogger logger,
        [EnumeratorCancellation] CancellationToken ct)
    {
        int count = 0;
        try
        {
            await foreach (BaseEvent e in events.WithCancellation(ct))
            {
                // Modelin düşünme metni tarayıcıya gitmez; REASONING_START/END "düşünüyor" göstermek için yeter
                if (e is ReasoningMessageStartEvent or ReasoningMessageContentEvent or ReasoningMessageEndEvent)
                {
                    continue;
                }
                // rawEvent: sağlayıcının ham update'i; debug içindir, her delta'nın boyunu katlar
                e.RawEvent = null;
                count++;
                yield return e;
            }
        }
        finally
        {
            logger.LogInformation("AG-UI run closed: events={Count}, aborted={Aborted}", count, ct.IsCancellationRequested);
        }
    }
}

public sealed class AgUiRun;
