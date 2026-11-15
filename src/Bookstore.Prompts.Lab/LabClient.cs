using Microsoft.Extensions.AI;
using OllamaSharp;

namespace Bookstore.Prompts.Lab;

public static class LabClient
{
    public static string Endpoint { get; } =
        Environment.GetEnvironmentVariable("BOOKSTORE_LLM_ENDPOINT") ?? "http://localhost:11434";

    public static string Model { get; } =
        Environment.GetEnvironmentVariable("BOOKSTORE_LLM_MODEL") ?? "gemma3:4b";

    public static IChatClient Create(string? model = null) =>
        new OllamaApiClient(
            new HttpClient { BaseAddress = new Uri(Endpoint), Timeout = TimeSpan.FromMinutes(10) },
            model ?? Model);

    // Same settings for every run: no sampling noise, no thinking tokens
    public static ChatOptions Options() => new()
    {
        Temperature = 0,
        Seed = 7,
        Reasoning = new ReasoningOptions { Effort = ReasoningEffort.None },
    };
}
