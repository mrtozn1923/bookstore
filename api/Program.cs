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
    new HttpClient { BaseAddress = new Uri(endpoint), Timeout = TimeSpan.FromMinutes(10) }, model));
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHttpContextAccessor();
    chatBuilder.Use((inner, services) => new ChaosChatClient(inner, services.GetRequiredService<IHttpContextAccessor>()));
}

builder.Services.AddSingleton<ConversationStore>();
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
    "Store catalog notes. When you use a note, cite it with its number in square brackets, like [1]:\n" +
    Catalog.Notes();

app.MapPost("/chat/sse", IResult (ChatStreamRequest request, IChatClient chat, ConversationStore conversations,
    ILogger<Program> logger, CancellationToken ct) =>
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

    IAsyncEnumerable<ChatResponseUpdate> updates = chat.GetStreamingResponseAsync(messages, cancellationToken: ct);
    IAsyncEnumerable<SseItem<object>> events = ChatStreaming.ToSseAsync(updates, logger, answer =>
    {
        lock (history)
        {
            history.Add(userMessage);
            history.Add(new ChatMessage(ChatRole.Assistant, answer));
        }
    }, ct);

    return TypedResults.ServerSentEvents(
        events
            .Prepend(new SseItem<object>(Catalog.Sources(), "sources"))
            .Prepend(new SseItem<object>(new ConversationEvent(id), "conversation"))
            .WithHeartbeatAsync(TimeSpan.FromSeconds(5), ct));
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
