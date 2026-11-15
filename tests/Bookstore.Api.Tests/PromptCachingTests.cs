using System.Text.Json;
using Anthropic.Models.Messages;
using Bookstore.Api.Ai;
using Bookstore.Api.Support;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Tests;

public sealed class PromptCachingTests
{
    // Shapes follow the prompt caching and cache diagnostics pages; the second one is the documented system_changed example.
    private const string FirstTurn = """
        {"id":"msg_turn1","type":"message","role":"assistant","model":"claude-haiku-5-5",
         "content":[{"type":"text","text":"Returns are accepted within 14 days of delivery."}],
         "stop_reason":"end_turn","diagnostics":null,
         "usage":{"input_tokens":21,"cache_creation_input_tokens":1210,"cache_read_input_tokens":0,"output_tokens":14}}
        """;

    private const string SecondTurnSystemChanged = """
        {"id":"msg_01Xyz","type":"message","role":"assistant","model":"claude-haiku-5-5",
         "content":[{"type":"text","text":"..."}],
         "stop_reason":"end_turn",
         "usage":{"input_tokens":42,"cache_read_input_tokens":0,"cache_creation_input_tokens":41850,"output_tokens":210},
         "diagnostics":{"cache_miss_reason":{"type":"system_changed","cache_missed_input_tokens":41850}}}
        """;

    private static readonly SupportKnowledge Knowledge = SupportKnowledge.LoadEmbedded();

    private static (SupportChatService Service, FakeClaudeHandler Handler, ListLogger<SupportChatService> Log) Create(params string[] responses)
    {
        FakeClaudeHandler handler = new(responses);
        ListLogger<SupportChatService> log = new();
        SupportChatService service = new(handler.CreateClient(), new InMemoryConversationStore(), Knowledge,
            Options.Create(new ClaudeOptions { CacheDiagnostics = true }), log);
        return (service, handler, log);
    }

    [Fact]
    public void BuildRequest_PutsBreakpointsOnTheKnowledgeBlockAndTopLevel()
    {
        MessageCreateParams request = SupportChatService.BuildRequest(
            [new UserMessageParam { Content = "Can I return a book?" }], Knowledge, new ClaudeOptions());
        JsonElement body = JsonSerializer.SerializeToElement(request.RawBodyData);

        JsonElement system = body.GetProperty("system");
        Assert.False(system[0].TryGetProperty("cache_control", out _));
        Assert.Equal("""{"type":"ephemeral"}""", system[1].GetProperty("cache_control").GetRawText());
        Assert.Equal("""{"type":"ephemeral"}""", body.GetProperty("cache_control").GetRawText());
        Assert.False(body.TryGetProperty("diagnostics", out _));
    }

    [Fact]
    public async Task TwoTurns_SendTheSamePrefixAndChainDiagnostics()
    {
        (SupportChatService service, FakeClaudeHandler handler, _) = Create(FirstTurn, SecondTurnSystemChanged);
        Guid conversation = Guid.NewGuid();

        await service.SendAsync(conversation, "Can I return a book?", null, CancellationToken.None);
        await service.SendAsync(conversation, "What if it arrived damaged?", null, CancellationToken.None);

        JsonElement first = JsonDocument.Parse(handler.RequestBodies[0]).RootElement;
        JsonElement second = JsonDocument.Parse(handler.RequestBodies[1]).RootElement;

        // tools -> system -> messages: everything before the new turn must be byte-identical.
        Assert.Equal(first.GetProperty("system").GetRawText(), second.GetProperty("system").GetRawText());
        Assert.Equal(first.GetProperty("messages")[0].GetRawText(), second.GetProperty("messages")[0].GetRawText());
        Assert.Equal(3, second.GetProperty("messages").GetArrayLength());

        Assert.Equal("""{"previous_message_id":null}""", first.GetProperty("diagnostics").GetRawText());
        Assert.Equal("msg_turn1", second.GetProperty("diagnostics").GetProperty("previous_message_id").GetString());
    }

    [Fact]
    public async Task DocumentedSystemChangedDiagnostics_IsLoggedAsWarning()
    {
        (SupportChatService service, _, ListLogger<SupportChatService> log) = Create(SecondTurnSystemChanged);

        SupportAnswer answer = await service.SendAsync(Guid.NewGuid(), "Hello", null, CancellationToken.None);

        Assert.Equal(41850, answer.Usage.CacheWrite);
        Assert.Contains(log.Entries, e => e.Level == LogLevel.Warning
                                         && e.Message.Contains("reason=system_changed missedTokens=41850"));
    }

    [Fact]
    public void CategoryTree_IsTheSameWhateverTheInputOrder()
    {
        string[] paths = ["Fiction/Crime", "Children/Picture Books", "Fiction/Classics", "Fiction/Crime"];

        string a = SupportKnowledge.RenderCategoryTree(paths);
        string b = SupportKnowledge.RenderCategoryTree(paths.Reverse());

        Assert.Equal(a, b);
        Assert.Equal("- Children/Picture Books\n- Fiction/Classics\n- Fiction/Crime", a);
    }

    [Fact]
    public void OneHourTtl_IsSerializedAsDocumented()
    {
        CacheControlEphemeral oneHour = new() { Ttl = Ttl.Ttl1h };

        Assert.Equal("""{"type":"ephemeral","ttl":"1h"}""", JsonSerializer.Serialize(oneHour));
    }

    [Fact]
    public void EstimateUsd_PricesCacheReadsAndOneHourWrites()
    {
        // Sonnet 5.5: cache read $0.10, 5m write $2.50, 1h write $4 per million tokens.
        decimal reads = ClaudePricing.EstimateUsd("claude-sonnet-5-5", new TokenUsage(0, 0, 1_000_000, 0, 0));
        decimal oneHourWrites = ClaudePricing.EstimateUsd("claude-sonnet-5-5",
            new TokenUsage(0, 0, 0, CacheWrite: 1_000_000, 0, CacheWrite1h: 1_000_000));

        Assert.Equal(0.10m, reads);
        Assert.Equal(4.00m, oneHourWrites);
    }

    [Fact]
    public void CostModel_OneWriteThenReads_IsCheaperThanNoCache()
    {
        CacheScenario scenario = new(PrefixTokens: 40_000, SuffixTokens: 600, OutputTokens: 250, Requests: 1_000, CacheWrites: 1);

        decimal none = PromptCacheCostModel.WithoutCache("claude-sonnet-5-5", scenario);
        decimal cached = PromptCacheCostModel.WithCache("claude-sonnet-5-5", scenario);
        decimal broken = PromptCacheCostModel.WithCache("claude-sonnet-5-5", scenario with { CacheWrites = 1_000 });

        Assert.Equal(83.70m, none);
        Assert.Equal(7.796m, cached);
        Assert.True(broken > none);
    }
}
