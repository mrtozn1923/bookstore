using Bookstore.Assistant.Api;
using Microsoft.Extensions.AI;
using OllamaSharp;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Model adresi, model adı (ve bulut sağlayıcıda API key) yalnız sunucunun configuration'ında durur
string endpoint = builder.Configuration["Assistant:OllamaEndpoint"] ?? "http://localhost:11434";
string model = builder.Configuration["Assistant:Model"] ?? "qwen3:4b";
builder.Services.AddChatClient(new OllamaApiClient(
    new HttpClient { BaseAddress = new Uri(endpoint), Timeout = TimeSpan.FromMinutes(10) }, model));

WebApplication app = builder.Build();

// System prompt da sunucuda: tarayıcı yalnız kullanıcının mesajını gönderir
const string SystemPrompt =
    "You are the assistant of Bookstore, an online bookstore. Answer briefly and only about books, " +
    "orders and the store. If you are not sure, say so.";

app.MapPost("/chat/sse", (ChatStreamRequest request, IChatClient chat, ILogger<Program> logger, CancellationToken ct) =>
{
    List<ChatMessage> messages = [new(ChatRole.System, SystemPrompt), new(ChatRole.User, request.Message)];
    IAsyncEnumerable<ChatResponseUpdate> updates = chat.GetStreamingResponseAsync(messages, cancellationToken: ct);
    return TypedResults.ServerSentEvents(
        ChatStreaming.ToSseAsync(updates, logger, ct).WithHeartbeatAsync(TimeSpan.FromSeconds(5), ct));
});

app.Run();
