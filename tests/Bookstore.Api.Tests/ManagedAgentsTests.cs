using System.Text.Json;
using Bookstore.Api.Agents;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bookstore.Api.Tests;

public sealed class ManagedAgentsTests
{
    // The documented create-agent response, with our name and model.
    private const string CreatedAgent = """
        {"id":"agent_01HqR2k7vXbZ9mNpL3wYcT8f","type":"agent","name":"Bookstore issue digest",
         "model":{"id":"claude-sonnet-5-5","effort":{"type":"medium"},"speed":"standard"},
         "system":"You write a short morning digest of Bookstore's open issues.","description":null,
         "tools":[{"type":"agent_toolset_20260401","default_config":{"permission_policy":{"type":"always_allow"}}}],
         "skills":[],"mcp_servers":[],"multiagent":null,"metadata":{},"version":1,
         "created_at":"2026-11-15T18:24:10.412Z","updated_at":"2026-11-15T18:24:10.412Z","archived_at":null}
        """;

    // Only the fields this test reads; the id prefix follows the documentation.
    private const string CreatedSession = """
        {"id":"sesn_01digest_test","type":"session","status":"running","environment_id":"env_01digest_test",
         "title":"Issue digest 2026-11-16","created_at":"2026-11-16T03:00:00Z","updated_at":"2026-11-16T03:00:00Z"}
        """;

    [Fact]
    public async Task Ensure_CreatesTheAgentOnceAndReusesIt()
    {
        FakeClaudeHandler handler = new(CreatedAgent);
        IssueDigestAgent digest = new(handler.CreateClient(), new InMemoryAgentRegistry(), NullLogger<IssueDigestAgent>.Instance);

        AgentReference first = await digest.EnsureAsync(CancellationToken.None);
        AgentReference second = await digest.EnsureAsync(CancellationToken.None);

        Assert.Equal(new AgentReference("agent_01HqR2k7vXbZ9mNpL3wYcT8f", 1), first);
        Assert.Equal(first, second);
        Assert.Single(handler.Requests);
        Assert.Equal("/v1/agents", handler.Requests[0].RequestUri!.AbsolutePath);
        // The SDK adds the Managed Agents beta header by itself.
        Assert.Contains("managed-agents-2026-04-01", handler.Requests[0].Headers.GetValues("anthropic-beta"));

        JsonElement body = JsonDocument.Parse(handler.RequestBodies[0]).RootElement;
        Assert.Equal("""[{"name":"web_search","enabled":false},{"name":"web_fetch","enabled":false}]""",
            body.GetProperty("tools")[0].GetProperty("configs").GetRawText());
    }

    [Fact]
    public async Task Start_PinsTheAgentVersionAndSeedsTheFirstMessage()
    {
        FakeClaudeHandler handler = new(CreatedAgent, CreatedSession);
        IssueDigestAgent digest = new(handler.CreateClient(), new InMemoryAgentRegistry(), NullLogger<IssueDigestAgent>.Instance);

        string sessionId = await digest.StartAsync("env_01digest_test", "vlt_01digest_test", new DateOnly(2026, 11, 16), CancellationToken.None);

        Assert.Equal("sesn_01digest_test", sessionId);
        Assert.Equal("/v1/sessions", handler.Requests[1].RequestUri!.AbsolutePath);
        JsonElement body = JsonDocument.Parse(handler.RequestBodies[1]).RootElement;
        Assert.Equal("""{"type":"agent","id":"agent_01HqR2k7vXbZ9mNpL3wYcT8f","version":1}""", body.GetProperty("agent").GetRawText());
        Assert.Equal("vlt_01digest_test", body.GetProperty("vault_ids")[0].GetString());
        Assert.Equal("user.message", body.GetProperty("initial_events")[0].GetProperty("type").GetString());
    }
}
