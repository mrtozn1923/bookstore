using System.Collections.Concurrent;
using Anthropic;
using Anthropic.Models.Beta.Agents;
using Anthropic.Models.Beta.Sessions;
using Anthropic.Models.Beta.Sessions.Events;

namespace Bookstore.Api.Agents;

public sealed record AgentReference(string Id, int Version);

// Agents are created once and reused. Keep the id somewhere durable (or in claude-lock.json with ant apply).
public interface IAgentRegistry
{
    AgentReference? Find(string key);
    void Save(string key, AgentReference agent);
}

public sealed class InMemoryAgentRegistry : IAgentRegistry
{
    private readonly ConcurrentDictionary<string, AgentReference> _agents = new();

    public AgentReference? Find(string key) => _agents.TryGetValue(key, out AgentReference? a) ? a : null;
    public void Save(string key, AgentReference agent) => _agents[key] = agent;
}

public sealed class IssueDigestAgent(IAnthropicClient claude, IAgentRegistry registry, ILogger<IssueDigestAgent> logger)
{
    public const string RegistryKey = "issue-digest";

    public static AgentCreateParams BuildAgent() => new()
    {
        Name = "Bookstore issue digest",
        Model = BetaManagedAgentsModel.ClaudeSonnet5_5,
        System = """
            You write a short morning digest of Bookstore's open issues for the development team.
            Group the issues by area (catalog, ordering, payments, support), list what changed since yesterday,
            and flag anything that blocks a release. Do not change, close or comment on any issue.
            Write the digest to /mnt/session/outputs/digest.md.
            """,
        Tools =
        [
            new BetaManagedAgentsAgentToolset20260401Params
            {
                // The digest needs the repository, not the web.
                Configs =
                [
                    new BetaManagedAgentsWebSearchToolConfigParams { Enabled = false },
                    new BetaManagedAgentsWebFetchToolConfigParams { Enabled = false },
                ],
            },
        ],
    };

    // Creates the agent only the first time; every later call returns the stored id and version.
    public async Task<AgentReference> EnsureAsync(CancellationToken cancellationToken)
    {
        if (registry.Find(RegistryKey) is { } existing)
        {
            return existing;
        }

        BetaManagedAgentsAgent created = await claude.Beta.Agents.Create(BuildAgent(), cancellationToken);
        AgentReference agent = new(created.ID, created.Version);
        registry.Save(RegistryKey, agent);
        logger.LogInformation("Created agent {AgentId} version {Version}", agent.Id, agent.Version);
        return agent;
    }

    // Pinning the version lets a new agent version roll out on purpose, not by accident.
    public static SessionCreateParams BuildSession(AgentReference agent, string environmentId, string vaultId, DateOnly day) => new()
    {
        Agent = new BetaManagedAgentsAgentParams { ID = agent.Id, Version = agent.Version },
        EnvironmentID = environmentId,
        // Credentials live in the vault and are injected outside the model's context; never in the prompt.
        VaultIds = [vaultId],
        Title = $"Issue digest {day:yyyy-MM-dd}",
        InitialEvents =
        [
            new BetaManagedAgentsUserMessageEventParams
            {
                Content = [new BetaManagedAgentsTextBlock { Text = $"Write the issue digest for {day:yyyy-MM-dd}." }],
            },
        ],
    };

    public async Task<string> StartAsync(string environmentId, string vaultId, DateOnly day, CancellationToken cancellationToken)
    {
        AgentReference agent = await EnsureAsync(cancellationToken);
        BetaManagedAgentsSession session = await claude.Beta.Sessions.Create(BuildSession(agent, environmentId, vaultId, day), cancellationToken);
        logger.LogInformation("Started session {SessionId} on agent {AgentId} v{Version}", session.ID, agent.Id, agent.Version);
        return session.ID;
    }
}
