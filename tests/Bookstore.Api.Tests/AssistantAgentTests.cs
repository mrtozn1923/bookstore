using System.Text.Json;
using Anthropic.Models.Messages;
using Bookstore.Api.Ai;
using Bookstore.Api.Assistant;
using Bookstore.Api.Catalog;
using Bookstore.Api.Ordering;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Tests;

public sealed class AssistantAgentTests
{
    // Shape of the tool_use response in the "Handle tool calls" page, with our two tools called in parallel.
    private const string ToolUseResponse = """
        {"id":"msg_01","type":"message","role":"assistant","model":"claude-haiku-5-5",
         "content":[
           {"type":"text","text":"Let me check both."},
           {"type":"tool_use","id":"toolu_01","name":"get_order_status","input":{"order_id":"1042"}},
           {"type":"tool_use","id":"toolu_02","name":"search_books","input":{"query":"dragon","max_price":350}}],
         "stop_reason":"tool_use","stop_sequence":null,"usage":{"input_tokens":900,"output_tokens":120}}
        """;

    private const string FinalResponse = """
        {"id":"msg_02","type":"message","role":"assistant","model":"claude-haiku-5-5",
         "content":[{"type":"text","text":"Order 1042 has shipped. The Hobbit is 310 TRY."}],
         "stop_reason":"end_turn","stop_sequence":null,"usage":{"input_tokens":1150,"output_tokens":40}}
        """;

    private static AssistantAgent CreateAgent(FakeClaudeHandler handler) => new(
        handler.CreateClient(),
        new BookstoreTools(new InMemoryBookRepository(), new InMemoryOrderRepository()),
        Options.Create(new ClaudeOptions()),
        NullLogger<AssistantAgent>.Instance);

    [Fact]
    public async Task AskAsync_RunsBothToolsAndSendsResultsInOneUserMessage()
    {
        FakeClaudeHandler handler = new(ToolUseResponse, FinalResponse);

        AgentAnswer answer = await CreateAgent(handler).AskAsync(
            "Where is order 1042, and do you have a dragon book under 350 TRY?",
            new CurrentCustomer(InMemoryOrderRepository.Ada), CancellationToken.None);

        Assert.Equal(ReplyOutcome.Complete, answer.Outcome);
        Assert.Equal(2, answer.Rounds);
        Assert.Equal(["get_order_status", "search_books"], answer.ToolCalls);

        JsonElement second = JsonDocument.Parse(handler.RequestBodies[1]).RootElement;
        JsonElement[] messages = second.GetProperty("messages").EnumerateArray().ToArray();
        Assert.Equal(["user", "assistant", "user"], messages.Select(m => m.GetProperty("role").GetString()));

        JsonElement[] results = messages[2].GetProperty("content").EnumerateArray().ToArray();
        Assert.All(results, r => Assert.Equal("tool_result", r.GetProperty("type").GetString()));
        Assert.Equal(["toolu_01", "toolu_02"], results.Select(r => r.GetProperty("tool_use_id").GetString()));
        Assert.Contains("Shipped", results[0].GetProperty("content").GetString());
        Assert.Contains("The Hobbit", results[1].GetProperty("content").GetString());
    }

    [Fact]
    public async Task AskAsync_AnotherCustomersOrder_IsAnErrorResultNotData()
    {
        FakeClaudeHandler handler = new(ToolUseResponse, FinalResponse);

        await CreateAgent(handler).AskAsync("Where is order 1042?",
            new CurrentCustomer(InMemoryOrderRepository.Alan), CancellationToken.None);

        JsonElement orderResult = JsonDocument.Parse(handler.RequestBodies[1]).RootElement
            .GetProperty("messages")[2].GetProperty("content")[0];
        Assert.True(orderResult.GetProperty("is_error").GetBoolean());
        Assert.DoesNotContain("Shipped", orderResult.GetProperty("content").GetString());
    }

    [Fact]
    public async Task AskAsync_SdkSendsApiVersionAndStrictTools()
    {
        FakeClaudeHandler handler = new(FinalResponse);

        await CreateAgent(handler).AskAsync("Hi", new CurrentCustomer(InMemoryOrderRepository.Ada), CancellationToken.None);

        Assert.Equal("2026-10-21", handler.Requests[0].Headers.GetValues("anthropic-version").Single());
        Assert.Equal("Bearer", handler.Requests[0].Headers.Authorization?.Scheme);

        JsonElement tools = JsonDocument.Parse(handler.RequestBodies[0]).RootElement.GetProperty("tools");
        Assert.True(tools[0].GetProperty("strict").GetBoolean());
        Assert.False(tools[0].GetProperty("input_schema").GetProperty("additionalProperties").GetBoolean());
        Assert.Equal("web_search_20260318", tools[2].GetProperty("type").GetString());
    }
}
