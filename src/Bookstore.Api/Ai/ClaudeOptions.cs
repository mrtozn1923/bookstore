using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Ai;

public sealed class ClaudeOptions
{
    public const string SectionName = "Claude";

    // When empty, the SDK reads the key from the ANTHROPIC_API_KEY environment variable.
    public string? ApiKey { get; set; }

    [Required]
    public string Model { get; set; } = "claude-haiku-5-5";

    [Range(1, 128_000)]
    public int MaxTokens { get; set; } = 400;

    [Range(0, 10)]
    public int MaxRetries { get; set; } = 2;

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);

    // Sends the diagnostics object so the API reports where the cached prefix diverged from the previous turn.
    public bool CacheDiagnostics { get; set; }
}
