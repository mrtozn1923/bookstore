using System.Text.Json;
using Bookstore.Prompts.Tasks.OrderAssistant;
using Microsoft.Extensions.AI;

namespace Bookstore.Prompts.Lab;

public static class AgentLab
{
    public const string Model = "qwen3:4b";

    static IChatClient CreateAgentClient() =>
        new ChatClientBuilder(LabClient.Create(Model))
            .UseFunctionInvocation(configure: f => f.MaximumIterationsPerRequest = 6)
            .Use(inner => new ThinkTagStripper(inner))
            .Build();

    public static List<ChatMessage> BuildMessages(string version, AssistantScenario scenario, int repeatHistory = 1)
    {
        string system = version == "v1" ? AssistantPrompts.V1 : AssistantPrompts.V2;
        List<ChatMessage> messages = [new(ChatRole.System, system)];
        // repeatHistory > 1 makes the same conversation longer, to see whether the rules fade
        messages.AddRange(Enumerable.Repeat(scenario.History, repeatHistory).SelectMany(h => h).Select(t => new ChatMessage(t.Role == "user" ? ChatRole.User : ChatRole.Assistant, t.Text)));
        if (version == "v3")
        {
            // The reminder goes right before the latest message, where a long history cannot bury it
            messages.Add(new(ChatRole.System, AssistantPrompts.Reminder));
        }
        messages.Add(new(ChatRole.User, scenario.Message));
        return messages;
    }

    public static async Task RunAsync(string version, bool approvalInCode, string? only = null, int repeatHistory = 1)
    {
        IChatClient client = CreateAgentClient();
        IReadOnlyList<AssistantScenario> scenarios =
            AssistantScenario.LoadJsonl(Path.Combine(AppContext.BaseDirectory, "data", "assistant-scenarios.jsonl"));
        string label = version + (approvalInCode ? "-approval" : "") + (repeatHistory > 1 ? $"-history{repeatHistory}" : "");
        Console.WriteLine($"order-assistant {label} | {Model} | {scenarios.Count} scenarios");
        int passed = 0;
        List<object> raw = [];
        foreach (AssistantScenario scenario in scenarios.Where(s => only is null || s.Id == only))
        {
            OrderStore store = OrderStore.Seed();
            ChatOptions options = LabClient.Options();
            options.Tools = OrderTools.Create(store, "c-ada", approvalInCode);

            ChatResponse response = await client.GetResponseAsync(BuildMessages(version, scenario, repeatHistory), options);

            List<string> calls = [.. response.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>().Select(c => c.Name)];
            List<string> approvals = [.. response.Messages.SelectMany(m => m.Contents).OfType<ToolApprovalRequestContent>()
                .Select(a => a.ToolCall is FunctionCallContent call ? call.Name : "?")];
            string finalText = response.Messages.LastOrDefault(m => m.Role == ChatRole.Assistant)?.Text.Trim() ?? "";
            List<string> issues = AssistantGrader.Grade(scenario.Expected, calls, finalText);
            int inputTokens = (int)(response.Usage?.InputTokenCount ?? 0);
            passed += issues.Count == 0 ? 1 : 0;

            Console.WriteLine($"{(issues.Count == 0 ? "PASS" : "FAIL")} {scenario.Id} calls=[{string.Join(", ", calls)}] changes=[{string.Join(", ", store.Changes)}]"
                + (repeatHistory > 1 ? $" input tokens={inputTokens}" : "")
                + (approvals.Count > 0 ? $" approval requested=[{string.Join(", ", approvals)}]" : "")
                + (issues.Count > 0 ? $" {string.Join("; ", issues)}" : ""));
            Console.WriteLine($"     {finalText.ReplaceLineEndings(" ")}");
            raw.Add(new
            {
                scenario.Id,
                Passed = issues.Count == 0,
                Issues = issues,
                Calls = response.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>()
                    .Select(c => new { c.Name, Arguments = c.Arguments }),
                Results = response.Messages.SelectMany(m => m.Contents).OfType<FunctionResultContent>().Select(r => r.Result?.ToString()),
                store.Changes,
                Approvals = approvals,
                FinalText = finalText,
            });
        }
        Console.WriteLine($"{passed}/{scenarios.Count(s => only is null || s.Id == only)} passed");
        ReportPrinter.SaveRaw($"order-assistant-{label}-{Model.Replace(':', '_')}", raw);
    }

    sealed record RawRun(string Id, List<RawCall> Calls, string FinalText);

    sealed record RawCall(string Name);

    // Re-grades saved raw outputs after a grader fix; no model calls
    public static void Regrade(string label)
    {
        string folder = Environment.GetEnvironmentVariable("BOOKSTORE_RUN_LOG") ?? ".";
        Dictionary<string, AssistantScenario> scenarios = AssistantScenario
            .LoadJsonl(Path.Combine(AppContext.BaseDirectory, "data", "assistant-scenarios.jsonl"))
            .ToDictionary(s => s.Id);
        JsonSerializerOptions json = new(JsonSerializerDefaults.Web);
        int passed = 0, total = 0;
        foreach (string line in File.ReadLines(Path.Combine(folder, $"order-assistant-{label}-{Model.Replace(':', '_')}.jsonl")))
        {
            RawRun run = JsonSerializer.Deserialize<RawRun>(line, json)!;
            List<string> issues = AssistantGrader.Grade(scenarios[run.Id].Expected, [.. run.Calls.Select(c => c.Name)], run.FinalText);
            total++;
            passed += issues.Count == 0 ? 1 : 0;
            Console.WriteLine($"{(issues.Count == 0 ? "PASS" : "FAIL")} {run.Id} calls=[{string.Join(", ", run.Calls.Select(c => c.Name))}] {string.Join("; ", issues)}");
        }
        Console.WriteLine($"{label}: {passed}/{total} passed (regraded)");
    }
}
