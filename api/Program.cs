using System.Net.ServerSentEvents;
using System.Threading.RateLimiting;
using Bookstore.Assistant.Api;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.AI;
using OllamaSharp;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Model adresi, model adı (ve bulut sağlayıcıda API key) yalnız sunucunun configuration'ında durur
string endpoint = builder.Configuration["Assistant:OllamaEndpoint"] ?? "http://localhost:11434";
string model = builder.Configuration["Assistant:Model"] ?? "qwen3:4b";
ChatClientBuilder chatBuilder = builder.Services.AddChatClient(new OllamaApiClient(
        new HttpClient { BaseAddress = new Uri(endpoint), Timeout = TimeSpan.FromMinutes(10) }, model))
    .UseFunctionInvocation(configure: f => f.MaximumIterationsPerRequest = 5);
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHttpContextAccessor();
    chatBuilder.Use((inner, services) => new ChaosChatClient(inner, services.GetRequiredService<IHttpContextAccessor>()));
}

builder.Services.AddSingleton<ConversationStore>();
builder.Services.AddSingleton<OrderBook>();
builder.Services.AddSingleton<AssistantTools>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<PendingApprovals>();
builder.Services.AddProblemDetails();
builder.Services.AddRateLimiter(options =>
{
    // Demo: istemci IP'si başına dakikada 10 istek; production'da kullanıcı kimliğine göre bölümle
    options.AddPolicy("chat", http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
    options.OnRejected = async (context, ct) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        }
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await TypedResults.Problem(title: "Too many requests", statusCode: 429,
                detail: "You have sent too many messages. Please wait before sending another one.")
            .ExecuteAsync(context.HttpContext);
    };
});

WebApplication app = builder.Build();
app.UseRateLimiter();

// System prompt da sunucuda: tarayıcı yalnız kullanıcının mesajını gönderir
string SystemPrompt =
    "You are the assistant of Bookstore, an online bookstore. Answer briefly and only about books, " +
    "orders and the store. If you are not sure, say so. You may use Markdown.\n" +
    "To recommend books, call show_books; do not list the books in text, write one short sentence instead.\n" +
    "Store catalog notes. When you use a note, cite it with its number in square brackets, like [1]:\n" +
    Catalog.Notes();

app.MapPost("/chat/sse", IResult (ChatStreamRequest request, IChatClient chat, ConversationStore conversations,
    AssistantTools tools, PendingApprovals pending, ILogger<Program> logger, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > 2000)
    {
        return TypedResults.Problem(title: "Invalid message", statusCode: 400,
            detail: "The message must be between 1 and 2000 characters.");
    }

    (Guid id, List<ChatMessage> history) = conversations.GetOrCreate(request.ConversationId);
    lock (history)
    {
        if (request.ReplaceLastTurn && history.Count >= 2)
        {
            history.RemoveRange(history.Count - 2, 2);
        }
    }

    ChatMessage userMessage = new(ChatRole.User, request.Message);
    List<ChatMessage> messages;
    lock (history)
    {
        messages = [new(ChatRole.System, SystemPrompt), .. history, userMessage];
    }

    IAsyncEnumerable<SseItem<object>> events = StreamTurn(id, history, userMessage, messages, chat, tools, pending, logger, ct)
        .Prepend(new SseItem<object>(Catalog.Sources(), "sources"))
        .Prepend(new SseItem<object>(new ConversationEvent(id), "conversation"));
    return TypedResults.ServerSentEvents(events.WithHeartbeatAsync(TimeSpan.FromSeconds(5), ct));
}).RequireRateLimiting("chat");

// Onay kartındaki karar: bekleyen turu onay cevabıyla birlikte modele geri verir ve akışı sürdürür
app.MapPost("/chat/approvals/{approvalId}", IResult (string approvalId, ApprovalDecision decision, IChatClient chat,
    AssistantTools tools, PendingApprovals pending, ILogger<Program> logger, CancellationToken ct) =>
{
    PendingApproval? item = pending.Take(approvalId);
    if (item is null)
    {
        return TypedResults.Problem(title: "Approval not found", statusCode: 404,
            detail: "This approval request has expired or was already answered.");
    }

    item.Messages.Add(new ChatMessage(ChatRole.User, [item.Request.CreateResponse(decision.Approved, decision.Reason)]));
    IAsyncEnumerable<SseItem<object>> events =
        StreamTurn(item.ConversationId, item.History, item.UserMessage, item.Messages, chat, tools, pending, logger, ct);
    return TypedResults.ServerSentEvents(events.WithHeartbeatAsync(TimeSpan.FromSeconds(5), ct));
}).RequireRateLimiting("chat");

app.MapGet("/chat/conversations/{id:guid}/messages", IResult (Guid id, ConversationStore conversations) =>
{
    List<ChatMessage>? history = conversations.Find(id);
    if (history is null)
    {
        return TypedResults.NotFound();
    }
    lock (history)
    {
        return TypedResults.Ok(history.Select(m => new HistoryItem(m.Role.Value, m.Text)).ToList());
    }
});

app.Run();

// Bir model turunu SSE event'lerine çevirir; onay istenirse turu bekletir, düzgün biterse geçmişe yazar
static IAsyncEnumerable<SseItem<object>> StreamTurn(Guid conversationId, List<ChatMessage> history, ChatMessage userMessage,
    List<ChatMessage> messages, IChatClient chat, AssistantTools tools, PendingApprovals pending, ILogger logger,
    CancellationToken ct)
{
    List<ChatResponseUpdate> received = [];
    IAsyncEnumerable<ChatResponseUpdate> updates = chat.GetStreamingResponseAsync(messages, tools.CreateOptions(), ct)
        .Select(update =>
        {
            received.Add(update);
            return update;
        });

    return ChatStreaming.ToSseAsync(updates, logger, answer =>
    {
        ToolApprovalRequestContent? approval = received.SelectMany(u => u.Contents).OfType<ToolApprovalRequestContent>().FirstOrDefault();
        if (approval is not null)
        {
            // Tur bitmedi: model ve tool çağrıları dahil bütün mesajlar onay gelene kadar bekler
            messages.AddMessages(received);
            pending.Save(approval, conversationId, history, userMessage, messages);
            return;
        }
        lock (history)
        {
            history.Add(userMessage);
            history.Add(new ChatMessage(ChatRole.Assistant, answer));
        }
    }, ct);
}
